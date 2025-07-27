using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services;

public interface IMultiParameterService
{
    Task<ExportResult> ProcessMultipleParametersAsync(MultiParameterRequest request);
    IEnumerable<ParameterCombination> GenerateAllCombinations(MultiParameterRequest request);
    int CalculateTotalCombinations(MultiParameterRequest request);
}

/// <summary>
/// Handles multi-parameter export processing with efficient combination generation
/// </summary>
public class MultiParameterService : IMultiParameterService
{
    private readonly IDatabaseService _databaseService;
    private readonly IExcelExportService _excelExportService;
    private readonly IConfigurationService _configService;

    public MultiParameterService(
        IDatabaseService databaseService,
        IExcelExportService excelExportService,
        IConfigurationService configService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
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
                   Port = port,
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
    /// Processes all parameter combinations sequentially (Phase 1 implementation)
    /// </summary>
    public async Task<ExportResult> ProcessMultipleParametersAsync(MultiParameterRequest request)
    {
        if (!request.IsValid)
        {
            throw new ArgumentException("Invalid request: FromMonthSerial and ToMonthSerial are required");
        }

        var stopwatch = Stopwatch.StartNew();
        var combinations = GenerateAllCombinations(request).ToList();
        var results = new List<FileExportResult>();

        TradeDataEXP.App.LogMessage($"Starting multi-parameter export: {combinations.Count} combinations");

        foreach (var combination in combinations)
        {
            var result = await ProcessSingleCombinationAsync(combination);
            results.Add(result);
            
            TradeDataEXP.App.LogMessage($"Processed combination {results.Count}/{combinations.Count}: {result.FileName} - {(result.Success ? "Success" : "Failed")}");
        }

        stopwatch.Stop();

        var exportResult = new ExportResult
        {
            TotalFiles = results.Count,
            SuccessfulFiles = results.Count(r => r.Success),
            FailedFiles = results.Count(r => !r.Success),
            TotalRecords = results.Sum(r => r.RecordCount),
            ProcessingTime = stopwatch.Elapsed,
            FileResults = results
        };

        TradeDataEXP.App.LogMessage($"Multi-parameter export completed: {exportResult.SuccessfulFiles}/{exportResult.TotalFiles} successful, {exportResult.TotalRecords} total records, {exportResult.ProcessingTime}");

        return exportResult;
    }

    /// <summary>
    /// Processes a single parameter combination following standard workflow
    /// </summary>
    private async Task<FileExportResult> ProcessSingleCombinationAsync(ParameterCombination combination)
    {
        var combinationStopwatch = Stopwatch.StartNew();
        
        try
        {
            // Convert combination to ExportParameters
            var parameters = combination.ToExportParameters();
            
            // Execute stored procedure
            await _databaseService.ExecuteExportDataStoredProcedureAsync(parameters);
            
            // Get data from EXPDATA view
            var data = await _databaseService.GetExportDataAsync(parameters);
            var dataList = data.ToList();
            
            // Generate filename using standard pattern
            var fileName = combination.GenerateFileName();
            
            if (!dataList.Any())
            {
                TradeDataEXP.App.LogMessage($"No data found for combination: {fileName}");
                return new FileExportResult
                {
                    Success = true,
                    FileName = fileName,
                    RecordCount = 0,
                    Message = "No data found for combination",
                    Parameters = combination,
                    ProcessingTime = combinationStopwatch.Elapsed
                };
            }
            
            // Export to Excel with existing formatting
            var filePath = await _excelExportService.ExportToExcelAsync(dataList, fileName);
            
            combinationStopwatch.Stop();
            
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
        catch (Exception ex)
        {
            combinationStopwatch.Stop();
            
            TradeDataEXP.App.LogMessage($"Error processing combination {combination.GenerateFileName()}: {ex.Message}");
            
            return new FileExportResult
            {
                Success = false,
                FileName = combination.GenerateFileName(),
                Message = $"Error: {ex.Message}",
                Parameters = combination,
                ProcessingTime = combinationStopwatch.Elapsed,
                Error = ex
            };
        }
    }
}
