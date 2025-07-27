using System;
using System.Collections.Generic;

namespace TradeDataEXP.Models
{
    /// <summary>
    /// Represents the result of file validation operations
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int RecordCount { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public TimeSpan ValidationTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Human-readable file size
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
                if (FileSizeBytes < 1024 * 1024 * 1024) return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
                return $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }
        
        /// <summary>
        /// Adds a validation error
        /// </summary>
        public void AddError(string error)
        {
            ValidationErrors.Add(error);
            IsValid = false;
        }
        
        /// <summary>
        /// Adds a validation warning
        /// </summary>
        public void AddWarning(string warning)
        {
            ValidationWarnings.Add(warning);
        }
        
        /// <summary>
        /// Gets summary of validation result
        /// </summary>
        public string GetSummary()
        {
            if (IsValid && ValidationWarnings.Count == 0)
                return $"✅ Valid - {RecordCount:N0} records, {FormattedFileSize}";
            
            if (IsValid && ValidationWarnings.Count > 0)
                return $"⚠️ Valid with {ValidationWarnings.Count} warning(s) - {RecordCount:N0} records, {FormattedFileSize}";
            
            return $"❌ Invalid - {ValidationErrors.Count} error(s), {ValidationWarnings.Count} warning(s)";
        }
    }

    /// <summary>
    /// Represents validation settings for file validation
    /// </summary>
    public class ValidationSettings
    {
        /// <summary>
        /// Maximum file size allowed (default: 100MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;
        
        /// <summary>
        /// Minimum number of records expected
        /// </summary>
        public int MinRecordCount { get; set; } = 1;
        
        /// <summary>
        /// Maximum number of records allowed (default: 1 million)
        /// </summary>
        public int MaxRecordCount { get; set; } = 1_000_000;
        
        /// <summary>
        /// Whether to validate Excel file structure
        /// </summary>
        public bool ValidateExcelStructure { get; set; } = true;
        
        /// <summary>
        /// Whether to validate data integrity
        /// </summary>
        public bool ValidateDataIntegrity { get; set; } = true;
        
        /// <summary>
        /// Required columns that must be present
        /// </summary>
        public List<string> RequiredColumns { get; set; } = new();
        
        /// <summary>
        /// Maximum validation time allowed (default: 30 seconds)
        /// </summary>
        public TimeSpan MaxValidationTime { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Recovery action for failed exports
    /// </summary>
    public enum RecoveryAction
    {
        Skip,
        Retry,
        RetryWithDifferentParams,
        ManualIntervention
    }

    /// <summary>
    /// Represents a recovery attempt for a failed export
    /// </summary>
    public class RecoveryAttempt
    {
        public int AttemptNumber { get; set; }
        public DateTime AttemptTime { get; set; } = DateTime.Now;
        public RecoveryAction Action { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool WasSuccessful { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
