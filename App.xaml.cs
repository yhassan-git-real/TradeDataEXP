using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using TradeDataEXP.Services;

namespace TradeDataEXP;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static IConfigurationService? _configService;
    private static IDatabaseService? _databaseService;
    private static IExcelExportService? _excelExportService;
    private static string? _logFilePath;

    public static IConfigurationService ConfigurationService
    {
        get
        {
            _configService ??= new ConfigurationService();
            return _configService;
        }
    }

    public static IDatabaseService DatabaseService
    {
        get
        {
            _databaseService ??= new DatabaseService(ConfigurationService);
            return _databaseService;
        }
    }

    public static IExcelExportService ExcelExportService
    {
        get
        {
            _excelExportService ??= new ExcelExportService(ConfigurationService);
            return _excelExportService;
        }
    }

    private static string LogFilePath 
    { 
        get
        {
            if (_logFilePath == null)
            {
                _configService ??= new ConfigurationService();
                _logFilePath = _configService.GetLogFilePath();
                
                var logDir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            return _logFilePath;
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            LogMessage("Application starting...");
            LogMessage($"Command line args: {string.Join(" ", e.Args)}");
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            LogMessage("Exception handlers registered");
            
            base.OnStartup(e);
            
            LogMessage("Creating and showing MainWindow manually...");
            
            var mainWindow = new TradeDataEXP.Views.MainWindow();
            mainWindow.Show();
            
            LogMessage("MainWindow created and shown");
            LogMessage("Application startup completed successfully");
        }
        catch (Exception ex)
        {
            LogMessage($"FATAL ERROR in OnStartup: {ex}");
            MessageBox.Show($"Application failed to start: {ex.Message}\n\nCheck log file: {LogFilePath}", 
                "TradeDataEXP - Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogMessage($"UNHANDLED UI EXCEPTION: {e.Exception}");
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nCheck log file: {LogFilePath}", 
            "TradeDataEXP - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        LogMessage($"UNHANDLED DOMAIN EXCEPTION: {ex}");
        MessageBox.Show($"A critical error occurred: {ex?.Message}\n\nCheck log file: {LogFilePath}", 
            "TradeDataEXP - Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void LogMessage(string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            Console.WriteLine(logEntry);
        }
        catch
        {
            // If logging fails, don't crash the app
        }
    }
}
