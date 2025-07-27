using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services;

/// <summary>
/// Enhanced logging service with separate log files for different operation types
/// Provides detailed logging for single exports, multi exports, and general operations
/// </summary>
public class EnhancedLoggingService : IEnhancedLoggingService
{
    private readonly string _logDirectory;
    private readonly string _dateStamp;
    
    private readonly string _generalLogFile;
    private readonly string _singleExportLogFile;
    private readonly string _multiExportLogFile;
    
    private readonly object _generalLock = new();
    private readonly object _singleLock = new();
    private readonly object _multiLock = new();

    public EnhancedLoggingService(IConfigurationService? configService = null)
    {
        // Use configuration service if provided, otherwise fall back to default
        if (configService != null)
        {
            _logDirectory = configService.GetValue("LOG_DIRECTORY", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
        }
        else
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        }
        
        _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        
        // Read log file names from .env file if config service is available
        if (configService != null)
        {
            var generalLogBase = configService.GetValue("LOG_FILENAME_BASE", "TradeDataEXP_Log");
            var singleLogBase = configService.GetValue("LOG_SINGLE_EXPORT_FILENAME", "TradeDataEXP_Log_SingleFile");
            var multiLogBase = configService.GetValue("LOG_MULTI_EXPORT_FILENAME", "TradeDataEXP_Log_MultiFiles");
            
            _generalLogFile = Path.Combine(_logDirectory, $"{generalLogBase}_{_dateStamp}.txt");
            _singleExportLogFile = Path.Combine(_logDirectory, $"{singleLogBase}_{_dateStamp}.txt");
            _multiExportLogFile = Path.Combine(_logDirectory, $"{multiLogBase}_{_dateStamp}.txt");
        }
        else
        {
            _generalLogFile = Path.Combine(_logDirectory, $"TradeDataEXP_Log_{_dateStamp}.txt");
            _singleExportLogFile = Path.Combine(_logDirectory, $"TradeDataEXP_Log_SingleFile_{_dateStamp}.txt");
            _multiExportLogFile = Path.Combine(_logDirectory, $"TradeDataEXP_Log_MultiFiles_{_dateStamp}.txt");
        }
        
        // Ensure log directory exists
        Directory.CreateDirectory(_logDirectory);
        
        // Log where files are being created
        Console.WriteLine($"Enhanced Logging Service initialized:");
        Console.WriteLine($"Log Directory: {_logDirectory}");
        Console.WriteLine($"General Log: {_generalLogFile}");
        Console.WriteLine($"Single Export Log: {_singleExportLogFile}");
        Console.WriteLine($"Multi Export Log: {_multiExportLogFile}");
        
        // Initialize log files with headers
        InitializeLogFiles();
    }

    private void InitializeLogFiles()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Debug: Log the file paths being used
        Console.WriteLine($"[DEBUG] Enhanced Logging Service Initializing:");
        Console.WriteLine($"[DEBUG] Log Directory: {_logDirectory}");
        Console.WriteLine($"[DEBUG] General Log: {_generalLogFile}");
        Console.WriteLine($"[DEBUG] Single Export Log: {_singleExportLogFile}");
        Console.WriteLine($"[DEBUG] Multi Export Log: {_multiExportLogFile}");
        
        // Initialize general log - SYSTEM EVENTS ONLY
        WriteToFile(_generalLogFile, $"[{timestamp}] ===============================================");
        WriteToFile(_generalLogFile, $"[{timestamp}] 🔧 SYSTEM: TradeDataEXP Application Started");
        WriteToFile(_generalLogFile, $"[{timestamp}] 🔧 SYSTEM: Enhanced Logging Service Initialized");
        WriteToFile(_generalLogFile, $"[{timestamp}] 🔧 SYSTEM: Log Directory: {_logDirectory}");
        WriteToFile(_generalLogFile, $"[{timestamp}] ===============================================");
        
        // Initialize single export log
        WriteToFile(_singleExportLogFile, $"[{timestamp}] ===============================================");
        WriteToFile(_singleExportLogFile, $"[{timestamp}] TradeDataEXP Single Export Operations Log");
        WriteToFile(_singleExportLogFile, $"[{timestamp}] ===============================================");
        
        // Initialize multi export log
        WriteToFile(_multiExportLogFile, $"[{timestamp}] ===============================================");
        WriteToFile(_multiExportLogFile, $"[{timestamp}] TradeDataEXP Multi Export Operations Log");
        WriteToFile(_multiExportLogFile, $"[{timestamp}] ===============================================");
    }

    public void LogGeneral(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        
        lock (_generalLock)
        {
            WriteToFile(_generalLogFile, logEntry);
        }
    }

    public void LogError(string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] ❌ ERROR: {message}";
        
        if (exception != null)
        {
            logEntry += $"\n[{timestamp}] Exception: {exception.Message}";
            logEntry += $"\n[{timestamp}] StackTrace: {exception.StackTrace}";
        }
        
        lock (_generalLock)
        {
            WriteToFile(_generalLogFile, logEntry);
        }
    }

    public void LogSystemEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] 🔧 SYSTEM: {message}";
        
        lock (_generalLock)
        {
            WriteToFile(_generalLogFile, logEntry);
        }
    }

    public void LogSingleExport(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        
        lock (_singleLock)
        {
            WriteToFile(_singleExportLogFile, logEntry);
        }
        // NO cross-logging to general - single export details stay in single log
    }

    public void LogMultiExport(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        
        lock (_multiLock)
        {
            WriteToFile(_multiExportLogFile, logEntry);
        }
        // NO cross-logging to general - multi export details stay in multi log
    }

    public void LogSingleExportParameters(string hsCode, string product, string exporterName, string iec,
        string foreignParty, string foreignCountry, string port, string fromMonthSerial, string toMonthSerial)
    {
        LogSingleExport("📋 =============== USER INPUT PARAMETERS ===============");
        LogSingleExport($"📋 HS Code: '{hsCode}' {(string.IsNullOrWhiteSpace(hsCode) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 Product: '{product}' {(string.IsNullOrWhiteSpace(product) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 Exporter: '{exporterName}' {(string.IsNullOrWhiteSpace(exporterName) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 IEC: '{iec}' {(string.IsNullOrWhiteSpace(iec) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 Foreign Party: '{foreignParty}' {(string.IsNullOrWhiteSpace(foreignParty) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 Foreign Country: '{foreignCountry}' {(string.IsNullOrWhiteSpace(foreignCountry) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 Port: '{port}' {(string.IsNullOrWhiteSpace(port) ? "(Empty - will use %)" : "")}");
        LogSingleExport($"📋 From Month: '{fromMonthSerial}'");
        LogSingleExport($"📋 To Month: '{toMonthSerial}'");
        LogSingleExport("📋 ================================================");
    }

    public void LogMultiExportParameters(string hsCode, string product, string exporterName, string iec,
        string foreignParty, string foreignCountry, string port, string fromMonthSerial, string toMonthSerial)
    {
        LogMultiExport("📋 =============== USER INPUT PARAMETERS (MULTI) ===============");
        LogMultiExport($"📋 HS Code(s): '{hsCode}' {(string.IsNullOrWhiteSpace(hsCode) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 Product(s): '{product}' {(string.IsNullOrWhiteSpace(product) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 Exporter(s): '{exporterName}' {(string.IsNullOrWhiteSpace(exporterName) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 IEC(s): '{iec}' {(string.IsNullOrWhiteSpace(iec) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 Foreign Party(s): '{foreignParty}' {(string.IsNullOrWhiteSpace(foreignParty) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 Foreign Country(s): '{foreignCountry}' {(string.IsNullOrWhiteSpace(foreignCountry) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 Port(s): '{port}' {(string.IsNullOrWhiteSpace(port) ? "(Empty - will use %)" : "(Will split by comma)")}");
        LogMultiExport($"📋 From Month: '{fromMonthSerial}'");
        LogMultiExport($"📋 To Month: '{toMonthSerial}'");
        LogMultiExport("📋 ========================================================");
    }

    public void LogAllCombinations(IEnumerable<ParameterCombination> combinations)
    {
        var combList = combinations.ToList();
        LogMultiExport($"🔄 =============== GENERATED COMBINATIONS ({combList.Count}) ===============");
        
        for (int i = 0; i < combList.Count; i++)
        {
            var combo = combList[i];
            LogMultiExport($"🔄 Combination {i + 1}: HS:{combo.HsCode}, Product:{combo.Product}, Exporter:{combo.Exporter}, IEC:{combo.IecCode}, ForeignParty:{combo.ForeignParty}, ForeignCountry:{combo.ForeignCountry}, Port:{combo.Port}");
        }
        
        LogMultiExport("🔄 ================================================================");
    }

    public void LogDatabaseQuery(string operationType, ExportParameters parameters, int recordCount)
    {
        Action<string> logMethod = operationType.ToUpper().Contains("SINGLE") ? LogSingleExport : LogMultiExport;
        
        logMethod($"🗄️ =============== DATABASE QUERY ({operationType}) ===============");
        logMethod($"🗄️ Stored Procedure Parameters:");
        logMethod($"🗄️   fromMonth: {parameters.FromMonthSerial}");
        logMethod($"🗄️   ToMonth: {parameters.ToMonthSerial}");
        logMethod($"🗄️   hs: {parameters.HsCode}");
        logMethod($"🗄️   prod: {parameters.Product}");
        logMethod($"🗄️   Iec: {parameters.Iec}");
        logMethod($"🗄️   ExpCmp: {parameters.ExporterName}");
        logMethod($"🗄️   forcount: {parameters.ForeignCountry}");
        logMethod($"🗄️   forname: {parameters.ForeignParty}");
        logMethod($"🗄️   port: {parameters.Port}");
        logMethod($"🗄️ Result: {recordCount} records returned");
        logMethod("🗄️ ================================================================");
    }

    private void WriteToFile(string filePath, string content)
    {
        try
        {
            File.AppendAllText(filePath, content + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Fallback to console if file writing fails
            Console.WriteLine($"[LOG ERROR] {ex.Message}: {content}");
        }
    }
}
