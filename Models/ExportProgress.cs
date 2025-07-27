using System;

namespace TradeDataEXP.Models
{
    /// <summary>
    /// Represents progress information for multi-parameter export operations
    /// </summary>
    public class ExportProgress
    {
        /// <summary>
        /// Current combination being processed (1-based)
        /// </summary>
        public int CurrentCombination { get; set; }

        /// <summary>
        /// Total number of combinations to process
        /// </summary>
        public int TotalCombinations { get; set; }

        /// <summary>
        /// Current operation being performed
        /// </summary>
        public string CurrentOperation { get; set; } = string.Empty;

        /// <summary>
        /// Current combination parameters (for display)
        /// </summary>
        public string CurrentParameters { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public double ProgressPercentage => TotalCombinations > 0 ? (double)CurrentCombination / TotalCombinations * 100 : 0;

        /// <summary>
        /// Number of successful exports
        /// </summary>
        public int SuccessfulExports { get; set; }

    /// <summary>
    /// Number of failed exports
    /// </summary>
    public int FailedExports { get; set; }

    /// <summary>
    /// Number of files that required recovery
    /// </summary>
    public int RecoveredExports { get; set; }

    /// <summary>
    /// Number of validation errors encountered
    /// </summary>
    public int ValidationErrors { get; set; }

    /// <summary>
    /// Time when the export started
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Current throughput (files per minute)
    /// </summary>
    public double Throughput { get; set; }
    
    /// <summary>
    /// Current memory usage in megabytes
    /// </summary>
    public double MemoryUsageMB { get; set; }        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                if (CurrentCombination <= 0 || TotalCombinations <= CurrentCombination)
                    return null;

                var elapsed = DateTime.Now - StartTime;
                var averageTimePerCombination = elapsed.TotalSeconds / CurrentCombination;
                var remainingCombinations = TotalCombinations - CurrentCombination;
                
                return TimeSpan.FromSeconds(averageTimePerCombination * remainingCombinations);
            }
        }

        /// <summary>
        /// Whether the export is complete
        /// </summary>
        public bool IsComplete => CurrentCombination >= TotalCombinations;

        /// <summary>
        /// Creates a copy of the current progress state
        /// </summary>
        public ExportProgress Clone()
        {
            return new ExportProgress
            {
                CurrentCombination = CurrentCombination,
                TotalCombinations = TotalCombinations,
                CurrentOperation = CurrentOperation,
                CurrentParameters = CurrentParameters,
                SuccessfulExports = SuccessfulExports,
                FailedExports = FailedExports,
                RecoveredExports = RecoveredExports,
                ValidationErrors = ValidationErrors,
                StartTime = StartTime,
                Throughput = Throughput,
                MemoryUsageMB = MemoryUsageMB
            };
        }
    }
}
