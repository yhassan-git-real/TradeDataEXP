using System;
using System.Collections.Generic;

namespace TradeDataEXP.Models;

/// <summary>
/// Result of processing a single parameter combination
/// </summary>
public class FileExportResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public ParameterCombination? Parameters { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Exception? Error { get; set; }
    
    // Compatibility properties for enhanced service
    public bool IsSuccess
    {
        get => Success;
        set => Success = value;
    }
    
    public string ErrorMessage
    {
        get => Message;
        set => Message = value;
    }
    
    public bool IsDataUnavailable { get; set; } = false;
}

/// <summary>
/// Overall result of multi-parameter export operation
/// </summary>
public class ExportResult
{
    public int TotalFiles { get; set; }
    public int SuccessfulFiles { get; set; }
    public int FailedFiles { get; set; }
    public int TotalRecords { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<FileExportResult> FileResults { get; set; } = new();
    
    // Enhanced properties for Phase 2
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime EndTime { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    private bool _isSuccessOverride = false;
    private bool _hasIsSuccessOverride = false;
    
    // Compatibility properties
    public int TotalCombinations
    {
        get => TotalFiles;
        set => TotalFiles = value;
    }
    
    public int SuccessfulExports
    {
        get => SuccessfulFiles;
        set => SuccessfulFiles = value;
    }
    
    public int FailedExports
    {
        get => FailedFiles;
        set => FailedFiles = value;
    }
    
    public bool IsSuccess 
    { 
        get => _hasIsSuccessOverride ? _isSuccessOverride : FailedFiles == 0;
        set
        {
            _isSuccessOverride = value;
            _hasIsSuccessOverride = true;
        }
    }
    
    public double SuccessRate => TotalFiles > 0 ? (double)SuccessfulFiles / TotalFiles * 100 : 0;
}
