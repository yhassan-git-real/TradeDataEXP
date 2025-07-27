using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeDataEXP.Models;
using TradeDataEXP.Services;

namespace TradeDataEXP.Services
{
    /// <summary>
    /// Enhanced multi-parameter service with parallel processing and detailed progress reporting
    /// Implements efficient parameter combination processing with modern performance optimizations
    /// </summary>
    public interface IEnhancedMultiParameterService
    {
        /// <summary>
        /// Process multiple parameter combinations with progress reporting and controlled concurrency
        /// </summary>
        Task<ExportResult> ProcessMultipleParametersAsync(
            MultiParameterRequest request,
            string outputDirectory,
            IProgress<ExportProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate optimal concurrency level based on system resources and combination count
        /// </summary>
        int CalculateOptimalConcurrency(int totalCombinations);
    }

    public class EnhancedMultiParameterService : IEnhancedMultiParameterService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelExportService _excelExportService;
        private readonly IConfigurationService _configurationService;
        private readonly IEnhancedLoggingService _enhancedLoggingService;

        public EnhancedMultiParameterService(
            IDatabaseService databaseService,
            IExcelExportService excelExportService,
            IConfigurationService configurationService,
            IEnhancedLoggingService enhancedLoggingService)
        {
            _databaseService = databaseService;
            _excelExportService = excelExportService;
            _configurationService = configurationService;
            _enhancedLoggingService = enhancedLoggingService;
        }

        public async Task<ExportResult> ProcessMultipleParametersAsync(
            MultiParameterRequest request,
            string outputDirectory,
            IProgress<ExportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            _enhancedLoggingService.LogMultiExport($"🚀 Enhanced multi-parameter export started. Total combinations: {request.TotalCombinations}");
            
            var result = new ExportResult
            {
                StartTime = startTime,
                TotalCombinations = request.TotalCombinations,
                OutputDirectory = outputDirectory
            };

            try
            {
                // Generate all parameter combinations
                _enhancedLoggingService.LogMultiExport("📋 Generating parameter combinations...");
                var combinations = GenerateAllCombinations(request).ToList();
                _enhancedLoggingService.LogMultiExport($"📋 Generated {combinations.Count} parameter combinations");
                
                // Log all combinations for debugging
                _enhancedLoggingService.LogAllCombinations(combinations);
                
                // Initialize progress tracking
                var exportProgress = new ExportProgress
                {
                    TotalCombinations = combinations.Count,
                    CurrentCombination = 0,
                    StartTime = startTime
                };

                // Process combinations sequentially for reliability
                _enhancedLoggingService.LogMultiExport("🔄 Starting sequential processing of combinations...");
                var fileResults = new List<FileExportResult>();
                
                for (int index = 0; index < combinations.Count; index++)
                {
                    var combination = combinations[index];
                    _enhancedLoggingService.LogMultiExport($"📊 Processing combination {index + 1}/{combinations.Count}: {combination.GetDisplayText()}");
                    
                    var fileResult = await ProcessSingleCombinationWithProgressAsync(
                        combination, 
                        outputDirectory, 
                        index + 1, 
                        exportProgress, 
                        progress, 
                        cancellationToken);
                    
                    if (fileResult != null)
                    {
                        fileResults.Add(fileResult);
                    }
                }

                _enhancedLoggingService.LogMultiExport($"✅ All combinations completed. Processing results...");
                
                // Aggregate results
                result.FileResults.AddRange(fileResults);
                result.SuccessfulExports = fileResults.Count(fr => fr?.IsSuccess == true);
                result.FailedExports = fileResults.Count(fr => fr?.IsSuccess == false && fr?.IsDataUnavailable != true);
                var noDataCount = fileResults.Count(fr => fr?.IsDataUnavailable == true);
                result.EndTime = DateTime.Now;
                result.IsSuccess = result.FailedExports == 0;

                _enhancedLoggingService.LogMultiExport($"📈 Export summary: {result.SuccessfulExports} successful, {result.FailedExports} failed, {noDataCount} skipped (no data)");

                // Final progress update
                exportProgress.CurrentCombination = combinations.Count;
                exportProgress.CurrentOperation = "Export completed";
                exportProgress.SuccessfulExports = result.SuccessfulExports;
                exportProgress.FailedExports = result.FailedExports;
                progress?.Report(exportProgress.Clone());

                return result;
            }
            catch (OperationCanceledException)
            {
                _enhancedLoggingService.LogMultiExport("⏹️ Enhanced multi-parameter export was cancelled");
                result.ErrorMessage = "Export operation was cancelled";
                result.IsSuccess = false;
                result.EndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                _enhancedLoggingService.LogError("Enhanced multi-parameter export failed", ex);
                result.ErrorMessage = $"Multi-parameter export failed: {ex.Message}";
                result.IsSuccess = false;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        private async Task<FileExportResult> ProcessSingleCombinationWithProgressAsync(
            ParameterCombination combination,
            string outputDirectory,
            int combinationNumber,
            ExportProgress sharedProgress,
            IProgress<ExportProgress>? progress,
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            try
            {
                _enhancedLoggingService.LogMultiExport($"📊 Starting processing of combination {combinationNumber}: {combination.GetDisplayText()}");
                
                // Thread-safe progress update
                lock (sharedProgress)
                {
                    sharedProgress.CurrentCombination = combinationNumber;
                    sharedProgress.CurrentOperation = $"Processing combination {combinationNumber}";
                    sharedProgress.CurrentParameters = combination.GetDisplayText();
                }
                progress?.Report(sharedProgress.Clone());

                // Convert to ExportParameters for database query
                var exportParams = combination.ToExportParameters();
                _enhancedLoggingService.LogMultiExport($"🔍 Converted to export parameters: HsCode={exportParams.HsCode}, Product={exportParams.Product}, Exporter={exportParams.ExporterName}, IEC={exportParams.Iec}, ForeignParty={exportParams.ForeignParty}, ForeignCountry={exportParams.ForeignCountry}, IndianPort={exportParams.IndianPort}, FromMonth={exportParams.FromMonthSerial}, ToMonth={exportParams.ToMonthSerial}");
                
                // Log database query with full details
                                // Execute stored procedure first
                _enhancedLoggingService.LogDatabaseQuery($"Multi Export - Combination {combinationNumber}", exportParams, 0);
                
                // Update progress - querying database
                lock (sharedProgress)
                {
                    sharedProgress.CurrentOperation = $"Querying database for combination {combinationNumber}";
                }
                progress?.Report(sharedProgress.Clone());

                // Execute stored procedure first
                _enhancedLoggingService.LogMultiExport($"🗄️ Executing stored procedure for combination {combinationNumber}...");
                await _databaseService.ExecuteExportDataStoredProcedureAsync(exportParams);
                
                // Then get data from EXPDATA view
                _enhancedLoggingService.LogMultiExport($"🗄️ Querying EXPDATA view for combination {combinationNumber}...");
                var data = await _databaseService.GetExportDataAsync();
                var recordCount = data?.Count() ?? 0;
                _enhancedLoggingService.LogMultiExport($"🗄️ Database query returned {recordCount} records for combination {combinationNumber}");
                
                if (data?.Any() != true)
                {
                    _enhancedLoggingService.LogMultiExport($"⏭️ No data found for combination {combinationNumber}: {combination.GetDisplayText()}");
                    _enhancedLoggingService.LogMultiExport($"🔄 Skipping Excel file creation and moving to next combination");
                    // When no records found, continue to next (not counted as failed)
                    // Implementation: "If (record = 0) Then ActiveWorkbook.Close True; Application.Quit" 
                    // This is normal behavior, not a failure - just skip and continue
                    
                    return new FileExportResult
                    {
                        FileName = combination.GenerateFileName(),
                        Parameters = combination,
                        IsSuccess = false,
                        ErrorMessage = $"No data available for combination: HS:{combination.HsCode}, Product:{combination.Product}, Exporter:{combination.Exporter}, IEC:{combination.IecCode}, ForeignParty:{combination.ForeignParty}, ForeignCountry:{combination.ForeignCountry}, IndianPort:{combination.IndianPort}. This is normal - skipped to next combination.",
                        ProcessingTime = DateTime.Now - startTime,
                        RecordCount = 0,
                        IsDataUnavailable = true  // Flag to indicate this is data unavailability, not a processing error
                    };
                }

                // Update progress - creating Excel file
                lock (sharedProgress)
                {
                    sharedProgress.CurrentOperation = $"Creating Excel file for combination {combinationNumber}";
                }
                progress?.Report(sharedProgress.Clone());

                // Generate filename (using established pattern)
                var fileName = combination.GenerateFileName();
                var filePath = Path.Combine(outputDirectory, fileName);
                _enhancedLoggingService.LogMultiExport($"📄 Creating Excel file: {fileName} at {filePath}");

                // Create Excel file (same service as single export)
                var processingStart = DateTime.Now;
                string actualFilePath;
                try
                {
                    actualFilePath = await _excelExportService.ExportToExcelAsync(data, filePath);
                    var excelProcessingTime = DateTime.Now - processingStart;
                    _enhancedLoggingService.LogMultiExport($"✅ Excel file created successfully for combination {combinationNumber} in {excelProcessingTime.TotalMilliseconds:F0}ms, Records: {recordCount}");
                }
                catch (Exception excelEx)
                {
                    _enhancedLoggingService.LogMultiExport($"❌ Excel export failed for combination {combinationNumber}: {excelEx.Message}");
                    throw;
                }
                var processingTime = DateTime.Now - processingStart;

                // Simple file existence check
                if (!File.Exists(actualFilePath) || new FileInfo(actualFilePath).Length == 0)
                {
                    lock (sharedProgress)
                    {
                        sharedProgress.FailedExports++;
                    }

                    return new FileExportResult
                    {
                        FileName = fileName,
                        Parameters = combination,
                        IsSuccess = false,
                        ErrorMessage = "File was not created or is empty",
                        ProcessingTime = processingTime
                    };
                }

                // Success
                lock (sharedProgress)
                {
                    sharedProgress.SuccessfulExports++;
                }

                return new FileExportResult
                {
                    FileName = Path.GetFileName(actualFilePath),
                    FilePath = actualFilePath,
                    Parameters = combination,
                    IsSuccess = true,
                    RecordCount = recordCount,
                    ProcessingTime = processingTime,
                    Message = $"Export successful. Records: {recordCount}"
                };
            }
            catch (OperationCanceledException)
            {
                _enhancedLoggingService.LogMultiExport($"⏹️ Processing cancelled for combination {combinationNumber}");
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                _enhancedLoggingService.LogMultiExport($"❌ Error processing combination {combinationNumber}: {ex.Message}");
                _enhancedLoggingService.LogMultiExport($"❌ Stack trace: {ex.StackTrace}");
                
                // Handle individual combination failure
                lock (sharedProgress)
                {
                    sharedProgress.FailedExports++;
                }

                return new FileExportResult
                {
                    FileName = combination.GenerateFileName(),
                    Parameters = combination,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = DateTime.Now - startTime
                };
            }
        }

        private IEnumerable<ParameterCombination> GenerateAllCombinations(MultiParameterRequest request)
        {
            // Modern LINQ implementation of nested loop parameter combinations
            // Implementation: For each HSCode, for each Exporter, for each Importer, etc.
            
            var hsCodeList = request.HsCodes;
            var productList = request.Products;
            var exporterList = request.Exporters;
            var portList = request.Ports;
            var iecCodeList = request.IecCodes;
            var foreignCountryList = request.ForeignCountries;
            var foreignPartyList = request.ForeignParties;

            // Generate cartesian product (all combinations) using nested loops concept
            return from hsCode in hsCodeList
                   from product in productList
                   from exporter in exporterList
                   from port in portList
                   from iecCode in iecCodeList
                   from foreignCountry in foreignCountryList
                   from foreignParty in foreignPartyList
                   select new ParameterCombination
                   {
                       HsCode = hsCode,
                       Product = product,
                       Exporter = exporter,
                       IndianPort = port,
                       IecCode = iecCode,
                       ForeignCountry = foreignCountry,
                       ForeignParty = foreignParty,
                       FromMonth = request.FromMonthSerial,
                       ToMonth = request.ToMonthSerial
                   };
        }

        public int CalculateOptimalConcurrency(int totalCombinations)
        {
            // Conservative approach - balance performance with resource usage
            var processorCount = Environment.ProcessorCount;
            
            if (totalCombinations <= 10)
            {
                // Small batches: use minimal concurrency to avoid overhead
                return Math.Min(2, processorCount);
            }
            else if (totalCombinations <= 100)
            {
                // Medium batches: use moderate concurrency
                return Math.Min(processorCount / 2, 4);
            }
            else
            {
                // Large batches: use higher concurrency but cap it
                return Math.Min(processorCount - 1, 8);
            }
        }

        /// <summary>
        /// Helper method to check what HS codes are available in database
        /// </summary>
        public async Task<string> GetAvailableHsCodesAsync()
        {
            try
            {
                var data = await _databaseService.GetExportDataAsync(maxRows: 1000);
                var hsCodes = data.Select(d => {
                    // Use dynamic property access for ExportData
                    dynamic dynData = d;
                    return dynData.HS_CODE?.ToString() ?? "";
                }).Where(hs => !string.IsNullOrEmpty(hs)).Distinct().OrderBy(hs => hs).Take(20);
                return string.Join(", ", hsCodes);
            }
            catch (Exception ex)
            {
                _enhancedLoggingService.LogMultiExport($"⚠️ Could not retrieve available HS codes: {ex.Message}");
                return "Could not determine available HS codes";
            }
        }
    }
}
