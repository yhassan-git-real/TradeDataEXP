using System;
using System.Collections.Generic;

namespace TradeDataEXP.Services;

/// <summary>
/// Enhanced logging service with separate log files for different operation types
/// General log: System events, errors, application lifecycle
/// Single/Multi logs: Specific operation details only
/// </summary>
public interface IEnhancedLoggingService
{
    /// <summary>
    /// Log system-level messages to general application log (errors, startup, shutdown, etc.)
    /// </summary>
    void LogGeneral(string message);
    
    /// <summary>
    /// Log application errors to general log
    /// </summary>
    void LogError(string message, Exception? exception = null);
    
    /// <summary>
    /// Log application startup/shutdown events to general log
    /// </summary>
    void LogSystemEvent(string message);
    
    /// <summary>
    /// Log message to single export operation log ONLY
    /// </summary>
    void LogSingleExport(string message);
    
    /// <summary>
    /// Log message to multi export operation log ONLY
    /// </summary>
    void LogMultiExport(string message);
    
    /// <summary>
    /// Log user input parameters for single export - ALL parameters
    /// </summary>
    void LogSingleExportParameters(string hsCode, string product, string exporterName, string iec, 
        string foreignParty, string foreignCountry, string port, string fromMonthSerial, string toMonthSerial);
    
    /// <summary>
    /// Log user input parameters for multi export - ALL parameters with lists
    /// </summary>
    void LogMultiExportParameters(string hsCode, string product, string exporterName, string iec,
        string foreignParty, string foreignCountry, string port, string fromMonthSerial, string toMonthSerial);
    
    /// <summary>
    /// Log all generated combinations for multi export
    /// </summary>
    void LogAllCombinations(IEnumerable<TradeDataEXP.Models.ParameterCombination> combinations);
    
    /// <summary>
    /// Log database query execution with full parameters
    /// </summary>
    void LogDatabaseQuery(string operationType, TradeDataEXP.Models.ExportParameters parameters, int recordCount);
}
