using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services;

public interface IMultiParameterService
{
    Task<ExportResult> ProcessMultipleParametersAsync(MultiParameterRequest request);
    
    /// <summary>
    /// Process multiple parameter combinations with progress reporting and controlled concurrency
    /// </summary>
    Task<ExportResult> ProcessMultipleParametersAsync(
        MultiParameterRequest request,
        string outputDirectory,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
    IEnumerable<ParameterCombination> GenerateAllCombinations(MultiParameterRequest request);
    int CalculateTotalCombinations(MultiParameterRequest request);
    /// <summary>
    /// Calculate optimal concurrency level based on system resources and combination count
    /// </summary>
    int CalculateOptimalConcurrency(int totalCombinations);
}

/// <summary>
/// Handles multi-parameter export processing with efficient combination generation
/// </summary>
public class MultiParameterService : IMultiParameterService
{
    private readonly IDatabaseService _databaseService;
    private readonly IExcelExportService _excelExportService;
    private readonly IConfigurationService _configService;
    private readonly IEnhancedLoggingService? _loggingService;

    public MultiParameterService(
        IDatabaseService databaseService,
        IExcelExportService excelExportService,
        IConfigurationService configService,
        IEnhancedLoggingService? loggingService = null)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _loggingService = loggingService;
    }

    /// <summary>
    /// Generates all possible combinations using LINQ (following nested loop logic)
    /// </summary>
    public IEnumerable<ParameterCombination> GenerateAllCombinations(MultiParameterRequest request)
    {
        return from hsCode in request.HsCodes
               from product in request.Products
               from exporter in request.Exporters
               from port in request.Ports
               from iec in request.IecCodes
               from country in request.ForeignCountries
               from party in request.ForeignParties
               select new ParameterCombination
               {
                   HsCode = hsCode,
                   Product = product,
                   Exporter = exporter,
                   IndianPort = port,
                   IecCode = iec,
                   ForeignCountry = country,
                   ForeignParty = party,
                   FromMonth = request.FromMonthSerial,
                   ToMonth = request.ToMonthSerial
               };
    }

    public int CalculateTotalCombinations(MultiParameterRequest request)
    {
        return request.TotalCombinations;
    }
    
    /// <summary>
    /// Calculate optimal concurrency level based on system resources and combination count
    /// </summary>
    public int CalculateOptimalConcurrency(int totalCombinations)
    {
        // Get processor count for base calculation
        int processorCount = Environment.ProcessorCount;
        
        // Base concurrency on processor count and combination count
        int concurrency;
        
        if (totalCombinations <= 10)
        {
            // For small jobs, limit concurrency to avoid overhead
            concurrency = Math.Min(totalCombinations, 2);
        }
        else if (totalCombinations <= 50)
        {
            // For medium jobs, use half of available processors
            concurrency = Math.Min(processorCount / 2, 4);
        }
        else
        {
            // For large jobs, use more processors but leave some headroom
            concurrency = Math.Max(1, processorCount - 1);
        }
        
        // Check if there's a configuration override
        string maxConcurrencyStr = _configService.GetValue("MAX_EXPORT_CONCURRENCY", string.Empty);
        if (!string.IsNullOrEmpty(maxConcurrencyStr) && int.TryParse(maxConcurrencyStr, out int configConcurrency))
        {
            // Use the smaller of calculated or configured value
            concurrency = Math.Min(concurrency, configConcurrency);
        }
        
        // Ensure we never exceed the total combinations
        concurrency = Math.Min(concurrency, totalCombinations);
        
        // Always ensure at least 1 thread
        return Math.Max(1, concurrency);
    }

    /// <summary>
    /// Processes all parameter combinations sequentially (Legacy implementation)
    /// </summary>
    public async Task<ExportResult> ProcessMultipleParametersAsync(MultiParameterRequest request)
    {
        // Call the enhanced version with default parameters
        return await ProcessMultipleParametersAsync(
            request, 
            _configService.GetValue("DEFAULT_OUTPUT_DIRECTORY", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports")),
            null,
            CancellationToken.None);
    }
    
    /// <summary>
    /// Enhanced implementation with progress reporting and concurrency control
    /// </summary>
    public async Task<ExportResult> ProcessMultipleParametersAsync(
        MultiParameterRequest request,
        string outputDirectory,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid)
        {
            throw new ArgumentException("Invalid request: FromMonthSerial and ToMonthSerial are required");
        }

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);
        
        // Log operation start
        _loggingService?.LogMultiExport($"Starting multi-parameter export to directory: {outputDirectory}");
        _loggingService?.LogMultiExportParameters(
            string.Join(",", request.HsCodes), 
            string.Join(",", request.Products),
            string.Join(",", request.Exporters), 
            string.Join(",", request.IecCodes),
            string.Join(",", request.ForeignParties), 
            string.Join(",", request.ForeignCountries),
            string.Join(",", request.Ports), 
            request.FromMonthSerial, 
            request.ToMonthSerial);

        var stopwatch = Stopwatch.StartNew();
        var combinations = GenerateAllCombinations(request).ToList();
        var results = new List<FileExportResult>();
        
        // Log all combinations
        _loggingService?.LogAllCombinations(combinations);
        
        // Initialize progress tracking
        var exportProgress = new ExportProgress
        {
            CurrentCombination = 0,
            TotalCombinations = combinations.Count,
            StartTime = DateTime.Now
        };
        
        // Calculate optimal concurrency
        int maxConcurrency = CalculateOptimalConcurrency(combinations.Count);
        _loggingService?.LogMultiExport($"Using concurrency level: {maxConcurrency}");
        
        // Use SemaphoreSlim to control concurrency
        using var concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task<FileExportResult>>();
        
        // Start processing combinations with controlled concurrency
        foreach (var combination in combinations)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Wait for a slot to become available
            await concurrencySemaphore.WaitAsync(cancellationToken);
            
            // Start processing this combination
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Process the combination
                    var result = await ProcessSingleCombinationWithProgressAsync(
                        combination, 
                        outputDirectory,
                        exportProgress, 
                        progress, 
                        cancellationToken);
                    
                    return result;
                }
                finally
                {
                    // Release the semaphore slot when done
                    concurrencySemaphore.Release();
                }
            }, cancellationToken));
        }
        
        // Wait for all tasks to complete
        var allResults = await Task.WhenAll(tasks);
        results.AddRange(allResults);
        
        stopwatch.Stop();
        
        // Update final progress
        exportProgress.CurrentCombination = combinations.Count;
        exportProgress.SuccessfulExports = results.Count(r => r.Success);
        exportProgress.FailedExports = results.Count(r => !r.Success);
        progress?.Report(exportProgress.Clone());
        
        // Create result object
        var exportResult = new ExportResult
        {
            TotalFiles = results.Count,
            SuccessfulFiles = results.Count(r => r.Success),
            FailedFiles = results.Count(r => !r.Success),
            TotalRecords = results.Sum(r => r.RecordCount),
            ProcessingTime = stopwatch.Elapsed,
            FileResults = results,
            StartTime = exportProgress.StartTime,
            EndTime = DateTime.Now,
            OutputDirectory = outputDirectory
        };
        
        // Log completion
        _loggingService?.LogMultiExport($"Multi-parameter export completed: {exportResult.SuccessfulFiles}/{exportResult.TotalFiles} successful, {exportResult.TotalRecords} total records");
        _loggingService?.LogOperationTotalTime("MultiExport", stopwatch.Elapsed, false);
        
        TradeDataEXP.App.LogMessage($"Multi-parameter export completed: {exportResult.SuccessfulFiles}/{exportResult.TotalFiles} successful, {exportResult.TotalRecords} total records, {exportResult.ProcessingTime}");

        return exportResult;
    }

    /// <summary>
    /// Processes a single parameter combination following standard workflow
    /// </summary>
    private async Task<FileExportResult> ProcessSingleCombinationAsync(ParameterCombination combination)
    {
        return await ProcessSingleCombinationWithProgressAsync(
            combination, 
            _configService.GetValue("DEFAULT_OUTPUT_DIRECTORY", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports")),
            null, 
            null, 
            CancellationToken.None);
    }
    
    /// <summary>
    /// Enhanced version of single combination processing with progress reporting
    /// </summary>
    private async Task<FileExportResult> ProcessSingleCombinationWithProgressAsync(
        ParameterCombination combination,
        string outputDirectory,
        ExportProgress? exportProgress = null,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var combinationStopwatch = Stopwatch.StartNew();
        var operationType = "MultiExport";
        var fileName = combination.GenerateFileName();
        
        // Update progress if provided
        if (exportProgress != null)
        {
            lock (exportProgress)
            {
                exportProgress.CurrentCombination++;
                exportProgress.CurrentOperation = "Processing";
                exportProgress.CurrentParameters = combination.GetDisplayText();
                exportProgress.MemoryUsageMB = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
                
                // Calculate throughput (files per minute)
                if (exportProgress.CurrentCombination > 1)
                {
                    var elapsedMinutes = (DateTime.Now - exportProgress.StartTime).TotalMinutes;
                    if (elapsedMinutes > 0)
                    {
                        exportProgress.Throughput = exportProgress.SuccessfulExports / elapsedMinutes;
                    }
                }
                
                // Report progress
                progress?.Report(exportProgress.Clone());
            }
        }
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            _loggingService?.StartStepTiming(operationType, $"Combination_{fileName}");
            _loggingService?.LogMultiExport($"Processing combination: {combination.GetDisplayText()}");
            
            // Convert combination to ExportParameters
            var parameters = combination.ToExportParameters();
            
            // Execute stored procedure
            _loggingService?.StartStepTiming(operationType, "ExecuteStoredProcedure");
            await _databaseService.ExecuteExportDataStoredProcedureAsync(parameters);
            _loggingService?.EndStepTiming(operationType, "ExecuteStoredProcedure");
            
            // Get data from EXPDATA view
            _loggingService?.StartStepTiming(operationType, "GetExportData");
            var data = await _databaseService.GetExportDataAsync(parameters);
            var dataList = data.ToList();
            _loggingService?.EndStepTiming(operationType, "GetExportData");
            _loggingService?.LogDatabaseQuery(operationType, parameters, dataList.Count);
            
            if (!dataList.Any())
            {
                _loggingService?.LogMultiExport($"No data found for combination: {fileName}");
                combinationStopwatch.Stop();
                
                // Update progress if provided
                if (exportProgress != null)
                {
                    lock (exportProgress)
                    {
                        exportProgress.CurrentOperation = "Completed (No Data)";
                        progress?.Report(exportProgress.Clone());
                    }
                }
                
                _loggingService?.EndStepTiming(operationType, $"Combination_{fileName}");
                
                return new FileExportResult
                {
                    Success = true,
                    FileName = fileName,
                    RecordCount = 0,
                    Message = "No data found for combination",
                    Parameters = combination,
                    ProcessingTime = combinationStopwatch.Elapsed,
                    IsDataUnavailable = true
                };
            }
            
            // Update progress if provided
            if (exportProgress != null)
            {
                lock (exportProgress)
                {
                    exportProgress.CurrentOperation = $"Exporting {dataList.Count} records to Excel";
                    progress?.Report(exportProgress.Clone());
                }
            }
            
            // Export to Excel with existing formatting
            _loggingService?.StartStepTiming(operationType, "ExportToExcel");
            var filePath = Path.Combine(outputDirectory, $"{fileName}.xlsx");
            
            // Check if file already exists
            if (File.Exists(filePath))
            {
                _loggingService?.LogMultiExport($"Warning: File already exists, will be overwritten: {filePath}");
            }
            
            filePath = await _excelExportService.ExportToExcelAsync(dataList, fileName);
            // Ensure the file is in the correct output directory
            if (Path.GetDirectoryName(filePath) != outputDirectory)
            {
                var newFilePath = Path.Combine(outputDirectory, Path.GetFileName(filePath));
                File.Copy(filePath, newFilePath, true);
                filePath = newFilePath;
            }
            _loggingService?.EndStepTiming(operationType, "ExportToExcel");
            
            combinationStopwatch.Stop();
            
            // Update progress if provided
            if (exportProgress != null)
            {
                lock (exportProgress)
                {
                    exportProgress.SuccessfulExports++;
                    exportProgress.CurrentOperation = "Completed Successfully";
                    progress?.Report(exportProgress.Clone());
                }
            }
            
            _loggingService?.LogMultiExport($"Successfully exported {dataList.Count} records to {filePath}");
            _loggingService?.EndStepTiming(operationType, $"Combination_{fileName}");
            
            return new FileExportResult
            {
                Success = true,
                FileName = fileName,
                FilePath = filePath,
                RecordCount = dataList.Count,
                Message = $"Successfully exported {dataList.Count} records",
                Parameters = combination,
                ProcessingTime = combinationStopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            combinationStopwatch.Stop();
            _loggingService?.LogMultiExport($"Operation cancelled for combination: {fileName}");
            _loggingService?.EndStepTiming(operationType, $"Combination_{fileName}");
            
            return new FileExportResult
            {
                Success = false,
                FileName = fileName,
                Message = "Operation was cancelled",
                Parameters = combination,
                ProcessingTime = combinationStopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            combinationStopwatch.Stop();
            
            // Update progress if provided
            if (exportProgress != null)
            {
                lock (exportProgress)
                {
                    exportProgress.FailedExports++;
                    exportProgress.CurrentOperation = "Failed";
                    progress?.Report(exportProgress.Clone());
                }
            }
            
            _loggingService?.LogMultiExport($"Error processing combination {fileName}: {ex.Message}");
            _loggingService?.EndStepTiming(operationType, $"Combination_{fileName}");
            
            TradeDataEXP.App.LogMessage($"Error processing combination {fileName}: {ex.Message}");
            
            return new FileExportResult
            {
                Success = false,
                FileName = fileName,
                Message = $"Error: {ex.Message}",
                Parameters = combination,
                ProcessingTime = combinationStopwatch.Elapsed,
                Error = ex
            };
        }
    }
}
