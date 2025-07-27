using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeDataEXP.Models;
using TradeDataEXP.Services;

namespace TradeDataEXP.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IExcelExportService _excelExportService;
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private ExportParameters parameters = new();

    [ObservableProperty]
    private bool isDarkMode = false;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string databaseStatus = string.Empty;

    public MainViewModel() : this(new ConfigurationService(), null, null)
    {
    }

    public MainViewModel(IConfigurationService configService, IDatabaseService? databaseService = null, IExcelExportService? excelExportService = null)
    {
        try
        {
            TradeDataEXP.App.LogMessage("MainViewModel constructor starting...");
            
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            // Initialize database status dynamically from configuration
            var server = _configService.GetValue("DB_SERVER", "Unknown");
            var database = _configService.GetValue("DB_NAME", "Unknown");
            var user = _configService.GetValue("DB_USER", "Unknown");
            DatabaseStatus = $"{server}/{database} • {user}";
            
            TradeDataEXP.App.LogMessage("Creating DatabaseService...");
            _databaseService = databaseService ?? new DatabaseService(_configService);
            TradeDataEXP.App.LogMessage("DatabaseService created");
            
            TestDatabaseConnection();
            
            TradeDataEXP.App.LogMessage("Database connection test initiated");
            
            TradeDataEXP.App.LogMessage("Creating ExcelExportService...");
            _excelExportService = excelExportService ?? new ExcelExportService(_configService);
            TradeDataEXP.App.LogMessage("ExcelExportService created");
            
            var now = DateTime.Now;
            TradeDataEXP.App.LogMessage($"Setting default date range for {now:yyyy-MM}");
            Parameters.FromMonthSerial = (now.Year * 100 + now.Month).ToString();
            Parameters.ToMonthSerial = (now.Year * 100 + now.Month).ToString();
            TradeDataEXP.App.LogMessage($"Default date range set: {Parameters.FromMonthSerial} to {Parameters.ToMonthSerial}");
            
            TradeDataEXP.App.LogMessage("MainViewModel constructor completed successfully");
        }
        catch (Exception ex)
        {
            TradeDataEXP.App.LogMessage($"ERROR in MainViewModel constructor: {ex}");
            throw;
        }
    }

    [RelayCommand]
    private async Task DownloadExcel()
    {
        if (!ValidateInputs())
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Executing stored procedure...";

            // Execute the stored procedure
            await _databaseService.ExecuteExportDataStoredProcedureAsync(Parameters);

            StatusMessage = "Fetching all data for export...";

            var data = await _databaseService.GetExportDataAsync();
            var dataList = data.ToList();

            if (!dataList.Any())
            {
                MessageBox.Show("No data found for the specified criteria.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "Generating Excel file...";

            var fileName = GenerateFileName();

            var filePath = await _excelExportService.ExportToExcelAsync(dataList, fileName);

            StatusMessage = $"Excel exported: {dataList.Count} records";
            
            var result = MessageBox.Show($"Excel file exported successfully!\n\nFile: {filePath}\n\nRecords: {dataList.Count:N0}\n\nWould you like to open the file?", 
                "Export Successful", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Parameters.Clear();
        StatusMessage = "Cleared";
        
        var now = DateTime.Now;
        Parameters.FromMonthSerial = (now.Year * 100 + now.Month).ToString();
        Parameters.ToMonthSerial = (now.Year * 100 + now.Month).ToString();
    }

    [RelayCommand]
    private void Reset()
    {
        Parameters.Clear();
        StatusMessage = "Reset to defaults";
        
        var now = DateTime.Now;
        Parameters.FromMonthSerial = (now.Year * 100 + now.Month).ToString();
        Parameters.ToMonthSerial = (now.Year * 100 + now.Month).ToString();
        
        TradeDataEXP.App.LogMessage("Form reset to default values");
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    private bool ValidateInputs()
    {
        if (!int.TryParse(Parameters.FromMonthSerial, out int fromMonth) || 
            !int.TryParse(Parameters.ToMonthSerial, out int toMonth) ||
            fromMonth <= 0 || toMonth <= 0)
        {
            MessageBox.Show("Please enter valid month serial numbers (YYYYMM format).", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (fromMonth > toMonth)
        {
            MessageBox.Show("From month cannot be greater than To month.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!IsValidMonthSerial(fromMonth) || !IsValidMonthSerial(toMonth))
        {
            MessageBox.Show("Month serial must be in YYYYMM format (e.g., 202401).", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private static bool IsValidMonthSerial(int monthSerial)
    {
        if (monthSerial < 100000 || monthSerial > 999999)
            return false;

        var year = monthSerial / 100;
        var month = monthSerial % 100;

        return year >= 2000 && year <= 2099 && month >= 1 && month <= 12;
    }

    private string GenerateFileName()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Parameters.HsCode) && Parameters.HsCode != "%")
            parts.Add(Parameters.HsCode.Replace("%", ""));

        if (!string.IsNullOrWhiteSpace(Parameters.Product) && Parameters.Product != "%")
            parts.Add(Parameters.Product.Replace(" ", "_").Replace("%", ""));

        if (!string.IsNullOrWhiteSpace(Parameters.ExporterName) && Parameters.ExporterName != "%")
            parts.Add(Parameters.ExporterName.Replace(" ", "_").Replace("%", ""));

        if (!string.IsNullOrWhiteSpace(Parameters.Port) && Parameters.Port != "%")
            parts.Add(Parameters.Port.Replace(" ", "_").Replace("%", ""));

        // Parse month serials for date range
        int.TryParse(Parameters.FromMonthSerial, out int fromMonthSerial);
        int.TryParse(Parameters.ToMonthSerial, out int toMonthSerial);

        var fromYear = fromMonthSerial / 100;
        var fromMonth = fromMonthSerial % 100;
        var toYear = toMonthSerial / 100;
        var toMonth = toMonthSerial % 100;

        string dateRange;
        if (fromMonthSerial == toMonthSerial)
        {
            dateRange = $"{GetMonthName(fromMonth)}{fromYear:00}";
        }
        else
        {
            dateRange = $"{GetMonthName(fromMonth)}{fromYear:00}-{GetMonthName(toMonth)}{toYear:00}";
        }

        parts.Add(dateRange);
        parts.Add("EXP");

        return string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
    }

    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "JAN", 2 => "FEB", 3 => "MAR", 4 => "APR",
            5 => "MAY", 6 => "JUN", 7 => "JUL", 8 => "AUG",
            9 => "SEP", 10 => "OCT", 11 => "NOV", 12 => "DEC",
            _ => "UNK"
        };
    }

    private async void TestDatabaseConnection()
    {
        try
        {
            TradeDataEXP.App.LogMessage("Testing real database connection...");
            
            // Actually test the database connection by executing a simple query
            var testData = await _databaseService.GetExportDataAsync(1);
            var testList = testData.ToList();
            
            if (testData != null && testData.Any())
            {
                // Real connection successful - show actual database info dynamically
                var server = _configService.GetValue("DB_SERVER", "Unknown");
                var database = _configService.GetValue("DB_NAME", "Unknown");
                var user = _configService.GetValue("DB_USER", "Unknown");
                DatabaseStatus = $"{server}/{database} • {user} ✓";
                TradeDataEXP.App.LogMessage($"Database connection test: SUCCESS - Connected to {DatabaseStatus}");
            }
            else
            {
                DatabaseStatus = "Connected • No Data";
                TradeDataEXP.App.LogMessage("Database connection test: SUCCESS but no data found");
            }
        }
        catch (Exception ex)
        {
            DatabaseStatus = "Connection Failed";
            TradeDataEXP.App.LogMessage($"Database connection test: FAILED - {ex.Message}");
        }
    }
}
