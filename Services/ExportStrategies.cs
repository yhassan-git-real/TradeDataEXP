using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services
{
    /// <summary>
    /// Advanced export strategy for intelligent batch processing and optimization
    /// </summary>
    public interface IExportStrategy
    {
        /// <summary>
        /// Process a batch of combinations with intelligent scheduling
        /// </summary>
        Task<List<FileExportResult>> ProcessBatchAsync(
            IEnumerable<ParameterCombination> combinations,
            string outputDirectory,
            IProgress<ExportProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Estimate processing time for a batch
        /// </summary>
        TimeSpan EstimateProcessingTime(int combinationCount);

        /// <summary>
        /// Get recommended batch size based on system resources
        /// </summary>
        int GetOptimalBatchSize(int totalCombinations);
    }

    /// <summary>
    /// Sequential processing strategy for small batches or limited resources
    /// </summary>
    public class SequentialExportStrategy : IExportStrategy
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelExportService _excelExportService;

        public SequentialExportStrategy(IDatabaseService databaseService, IExcelExportService excelExportService)
        {
            _databaseService = databaseService;
            _excelExportService = excelExportService;
        }

        public async Task<List<FileExportResult>> ProcessBatchAsync(
            IEnumerable<ParameterCombination> combinations,
            string outputDirectory,
            IProgress<ExportProgress> progress,
            CancellationToken cancellationToken)
        {
            var results = new List<FileExportResult>();
            var combinationList = combinations.ToList();

            for (int i = 0; i < combinationList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var result = await ProcessSingleCombinationAsync(
                    combinationList[i], 
                    outputDirectory, 
                    i + 1, 
                    combinationList.Count,
                    progress,
                    cancellationToken);
                
                results.Add(result);
            }

            return results;
        }

        public TimeSpan EstimateProcessingTime(int combinationCount)
        {
            // Conservative estimate: 3-5 seconds per combination for sequential processing
            return TimeSpan.FromSeconds(combinationCount * 4);
        }

        public int GetOptimalBatchSize(int totalCombinations)
        {
            // Sequential processing works best with smaller batches
            return Math.Min(totalCombinations, 10);
        }

        private async Task<FileExportResult> ProcessSingleCombinationAsync(
            ParameterCombination combination,
            string outputDirectory,
            int currentIndex,
            int totalCount,
            IProgress<ExportProgress> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // Report progress
                progress?.Report(new ExportProgress
                {
                    CurrentCombination = currentIndex,
                    TotalCombinations = totalCount,
                    CurrentOperation = $"Processing {combination.GetDisplayText()}",
                    CurrentParameters = combination.GetDisplayText()
                });

                // Convert and execute query
                var exportParams = combination.ToExportParameters();
                var data = await _databaseService.GetExportDataAsync(exportParams);

                if (data?.Any() != true)
                {
                    return new FileExportResult
                    {
                        FileName = combination.GenerateFileName(),
                        Parameters = combination,
                        IsSuccess = false,
                        ErrorMessage = "No data found for this combination",
                        ProcessingTime = TimeSpan.Zero
                    };
                }

                // Create Excel file
                var fileName = combination.GenerateFileName();
                var filePath = Path.Combine(outputDirectory, fileName);
                
                var processingStart = DateTime.Now;
                await _excelExportService.ExportToExcelAsync(data, filePath);
                var processingTime = DateTime.Now - processingStart;

                return new FileExportResult
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Parameters = combination,
                    IsSuccess = true,
                    RecordCount = data.Count(),
                    ProcessingTime = processingTime
                };
            }
            catch (Exception ex)
            {
                return new FileExportResult
                {
                    FileName = combination.GenerateFileName(),
                    Parameters = combination,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = TimeSpan.Zero
                };
            }
        }
    }

    /// <summary>
    /// Parallel processing strategy for medium to large batches
    /// </summary>
    public class ParallelExportStrategy : IExportStrategy
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelExportService _excelExportService;
        private readonly int _maxConcurrency;

        public ParallelExportStrategy(IDatabaseService databaseService, IExcelExportService excelExportService, int maxConcurrency = 4)
        {
            _databaseService = databaseService;
            _excelExportService = excelExportService;
            _maxConcurrency = maxConcurrency;
        }

        public async Task<List<FileExportResult>> ProcessBatchAsync(
            IEnumerable<ParameterCombination> combinations,
            string outputDirectory,
            IProgress<ExportProgress> progress,
            CancellationToken cancellationToken)
        {
            var combinationList = combinations.ToList();
            using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
            
            var processingTasks = combinationList.Select(async (combination, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Report progress for this thread
                    progress?.Report(new ExportProgress
                    {
                        CurrentCombination = index + 1,
                        TotalCombinations = combinationList.Count,
                        CurrentOperation = $"Processing {combination.GetDisplayText()}",
                        CurrentParameters = combination.GetDisplayText()
                    });

                    return await ProcessSingleCombinationAsync(combination, outputDirectory, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(processingTasks);
            return results.ToList();
        }

        public TimeSpan EstimateProcessingTime(int combinationCount)
        {
            // Parallel processing: divide by concurrency factor with some overhead
            var sequentialTime = combinationCount * 3; // 3 seconds per combination
            var parallelTime = sequentialTime / Math.Min(_maxConcurrency, combinationCount);
            return TimeSpan.FromSeconds(parallelTime + (combinationCount * 0.5)); // Add overhead
        }

        public int GetOptimalBatchSize(int totalCombinations)
        {
            // Parallel processing can handle larger batches efficiently
            return Math.Min(totalCombinations, _maxConcurrency * 10);
        }

        private async Task<FileExportResult> ProcessSingleCombinationAsync(
            ParameterCombination combination,
            string outputDirectory,
            CancellationToken cancellationToken)
        {
            try
            {
                var exportParams = combination.ToExportParameters();
                var data = await _databaseService.GetExportDataAsync(exportParams);

                if (data?.Any() != true)
                {
                    return new FileExportResult
                    {
                        FileName = combination.GenerateFileName(),
                        Parameters = combination,
                        IsSuccess = false,
                        ErrorMessage = "No data found for this combination",
                        ProcessingTime = TimeSpan.Zero
                    };
                }

                var fileName = combination.GenerateFileName();
                var filePath = Path.Combine(outputDirectory, fileName);
                
                var processingStart = DateTime.Now;
                await _excelExportService.ExportToExcelAsync(data, filePath);
                var processingTime = DateTime.Now - processingStart;

                return new FileExportResult
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Parameters = combination,
                    IsSuccess = true,
                    RecordCount = data.Count(),
                    ProcessingTime = processingTime
                };
            }
            catch (Exception ex)
            {
                return new FileExportResult
                {
                    FileName = combination.GenerateFileName(),
                    Parameters = combination,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ProcessingTime = TimeSpan.Zero
                };
            }
        }
    }
}
