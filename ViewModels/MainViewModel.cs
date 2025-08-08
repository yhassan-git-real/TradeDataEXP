using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    private readonly IMultiParameterService _multiParameterService;
    private readonly IEnhancedLoggingService _enhancedLoggingService;
    private CancellationTokenSource? _cancellationTokenSource;

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

    // Multi-parameter export progress properties
    [ObservableProperty]
    private bool isMultiExportInProgress = false;

    [ObservableProperty]
    private int multiExportProgress = 0;

    [ObservableProperty]
    private string multiExportStatus = string.Empty;

    [ObservableProperty]
    private string multiExportDetails = string.Empty;

    [ObservableProperty]
    private bool canCancelMultiExport = false;

    // Universal cancellation properties
    [ObservableProperty]
    private bool isAnyOperationInProgress = false;

    [ObservableProperty]
    private bool canCancelOperation = false;

    [ObservableProperty]
    private string currentOperationType = string.Empty;

    public MainViewModel() : this(new ConfigurationService(), null, null, null)
    {
    }

    public MainViewModel(IConfigurationService configService, IDatabaseService? databaseService = null, IExcelExportService? excelExportService = null, IMultiParameterService? multiParameterService = null)
    {
        try
        {
            TradeDataEXP.App.LogMessage("MainViewModel constructor starting...");
            
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            
            // Initialize enhanced logging service first
            TradeDataEXP.App.LogMessage("Creating EnhancedLoggingService...");
            _enhancedLoggingService = new EnhancedLoggingService(_configService);
            TradeDataEXP.App.LogMessage("EnhancedLoggingService created");
            
            _enhancedLoggingService.LogSystemEvent("MainViewModel initialization started");
            
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
            
            TradeDataEXP.App.LogMessage("Creating MultiParameterService...");
            _multiParameterService = multiParameterService ?? new MultiParameterService(_databaseService, _excelExportService, _configService, _enhancedLoggingService);
            TradeDataEXP.App.LogMessage("MultiParameterService created");
            
            var now = DateTime.Now;
            TradeDataEXP.App.LogMessage($"Setting default date range for {now:yyyy-MM}");
            Parameters.FromMonthSerial = (now.Year * 100 + now.Month).ToString();
            Parameters.ToMonthSerial = (now.Year * 100 + now.Month).ToString();
            TradeDataEXP.App.LogMessage($"Default date range set: {Parameters.FromMonthSerial} to {Parameters.ToMonthSerial}");
            
            TradeDataEXP.App.LogMessage("MainViewModel constructor completed successfully");
        }
        catch (Exception ex)
        {
            _enhancedLoggingService?.LogError("MainViewModel constructor failed", ex);
            TradeDataEXP.App.LogMessage($"ERROR in MainViewModel constructor: {ex}");
            throw;
        }
    }

    [RelayCommand]
    private async Task DownloadExcel()
    {
        if (!ValidateInputs())
            return;

        var operationType = "Single Export";
        var operationStartTime = DateTime.Now;

        try
        {
            // Log all user input parameters for single export
            _enhancedLoggingService.LogSingleExportParameters(
                Parameters.HsCode, Parameters.Product, Parameters.ExporterName, Parameters.Iec,
                Parameters.ForeignParty, Parameters.ForeignCountry, Parameters.IndianPort, 
                Parameters.FromMonthSerial, Parameters.ToMonthSerial);

            // Create new cancellation token for this operation
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Set operation state
            IsLoading = true;
            IsAnyOperationInProgress = true;
            CanCancelOperation = true;
            CurrentOperationType = operationType;
            StatusMessage = "Executing stored procedure...";

            // Start overall operation timing
            _enhancedLoggingService.LogSingleExport("🚀 Starting single export operation with cancellation support");

            // Step 1: Execute stored procedure
            _enhancedLoggingService.StartStepTiming(operationType, "Database Stored Procedure");
            await _databaseService.ExecuteExportDataStoredProcedureAsync(Parameters);
            _enhancedLoggingService.EndStepTiming(operationType, "Database Stored Procedure");

            // Check for cancellation
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            StatusMessage = "Fetching all data for export...";
            _enhancedLoggingService.LogSingleExport("📊 Fetching export data for single operation");

            // Step 2: Fetch data
            _enhancedLoggingService.StartStepTiming(operationType, "Data Retrieval");
            var data = await _databaseService.GetExportDataAsync();
            var dataList = data.ToList();
            _enhancedLoggingService.EndStepTiming(operationType, "Data Retrieval");
            
            // Log database query result
            _enhancedLoggingService.LogDatabaseQuery("Single Export", Parameters, dataList.Count);

            // Check for cancellation
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (!dataList.Any())
            {
                _enhancedLoggingService.LogSingleExport("❌ No data found for the specified criteria");
                var totalTime = DateTime.Now - operationStartTime;
                _enhancedLoggingService.LogOperationTotalTime(operationType, totalTime, true);
                MessageBox.Show("No data found for the specified criteria.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "Generating Excel file...";
            _enhancedLoggingService.LogSingleExport($"📄 Generating Excel file for {dataList.Count} records");

            // Step 3: Generate filename
            _enhancedLoggingService.StartStepTiming(operationType, "Filename Generation");
            var fileName = GenerateFileName();
            _enhancedLoggingService.EndStepTiming(operationType, "Filename Generation");

            // Check for cancellation before file creation
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            // Step 4: Excel file creation
            _enhancedLoggingService.StartStepTiming(operationType, "Excel File Creation");
            var filePath = await _excelExportService.ExportToExcelAsync(dataList, fileName);
            _enhancedLoggingService.EndStepTiming(operationType, "Excel File Creation");

            // Check for cancellation
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            StatusMessage = $"Excel exported: {dataList.Count} records";
            
            // Calculate and log total operation time
            var operationTotalTime = DateTime.Now - operationStartTime;
            _enhancedLoggingService.LogSingleExport($"✅ Single export completed successfully: {dataList.Count} records exported to {filePath}");
            _enhancedLoggingService.LogOperationTotalTime(operationType, operationTotalTime, true);
            
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
        catch (OperationCanceledException)
        {
            var cancelledTime = DateTime.Now - operationStartTime;
            StatusMessage = "Single export was cancelled";
            _enhancedLoggingService.LogSingleExport("⏹️ Single export operation was cancelled by user");
            _enhancedLoggingService.LogOperationTotalTime(operationType, cancelledTime, true);
            TradeDataEXP.App.LogMessage("⏹️ Single export operation was cancelled by user");
            MessageBox.Show("Single export operation was cancelled.", "Export Cancelled", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var errorTime = DateTime.Now - operationStartTime;
            _enhancedLoggingService.LogError("Single export operation failed", ex);
            _enhancedLoggingService.LogSingleExport($"❌ Single export failed: {ex.Message}");
            _enhancedLoggingService.LogOperationTotalTime(operationType, errorTime, true);
            StatusMessage = $"Error: {ex.Message}";
            TradeDataEXP.App.LogMessage($"❌ Single export failed: {ex.Message}");
            MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
            IsAnyOperationInProgress = false;
            CanCancelOperation = false;
            CurrentOperationType = string.Empty;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
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

    [RelayCommand]
    private async Task DownloadMultipleExcel()
    {
        if (!ValidateInputs())
            return;

        var operationType = "Multi-Export";
        var operationStartTime = DateTime.Now;

        try
        {
            // Log all user input parameters for multi export
            _enhancedLoggingService.LogMultiExportParameters(
                Parameters.HsCode, Parameters.Product, Parameters.ExporterName, Parameters.Iec,
                Parameters.ForeignParty, Parameters.ForeignCountry, Parameters.IndianPort, 
                Parameters.FromMonthSerial, Parameters.ToMonthSerial);

            // Create new cancellation token for this operation
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            IsLoading = true;
            IsMultiExportInProgress = true;
            IsAnyOperationInProgress = true;
            CanCancelMultiExport = true;
            CanCancelOperation = true;
            CurrentOperationType = operationType;
            StatusMessage = "Initializing multi-parameter export...";
            MultiExportStatus = "Calculating combinations...";
            MultiExportProgress = 0;

            _enhancedLoggingService.LogMultiExport("🚀 Starting enhanced multi-parameter export with cancellation support");

            // Step 1: Generate combinations
            _enhancedLoggingService.StartStepTiming(operationType, "Generate Parameter Combinations");
            var multiRequest = MultiParameterRequest.FromExportParameters(Parameters);
            
            // Log all generated combinations
            var allCombinations = GenerateAllCombinationsForLogging(multiRequest);
            _enhancedLoggingService.LogAllCombinations(allCombinations);
            var totalCombinations = multiRequest.TotalCombinations;
            _enhancedLoggingService.EndStepTiming(operationType, "Generate Parameter Combinations");

            if (totalCombinations > 100)
            {
                var confirmResult = MessageBox.Show(
                    $"This will generate {totalCombinations} files. This may take a long time.\n\n" +
                    $"Estimated processing time: {EstimateProcessingTime(totalCombinations)}\n\n" +
                    $"Do you want to continue?",
                    "Multiple File Export Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    StatusMessage = "Multi-parameter export cancelled";
                    var cancelledTime = DateTime.Now - operationStartTime;
                    _enhancedLoggingService.LogOperationTotalTime(operationType, cancelledTime, false);
                    return;
                }
            }

            // Step 2: Setup output directory
            _enhancedLoggingService.StartStepTiming(operationType, "Setup Output Directory");
            var outputDir = _configService.GetValue("OUTPUT_DIRECTORY", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            Directory.CreateDirectory(outputDir);
            _enhancedLoggingService.EndStepTiming(operationType, "Setup Output Directory");

            MultiExportStatus = $"Processing {totalCombinations} combinations...";
            MultiExportDetails = $"Using enhanced parallel processing • Concurrency: {_multiParameterService.CalculateOptimalConcurrency(totalCombinations)}";

            TradeDataEXP.App.LogMessage($"📊 Processing {totalCombinations} combinations using enhanced service");

            // Create progress reporter
            var progress = new Progress<ExportProgress>(progressInfo =>
            {
                MultiExportProgress = (int)progressInfo.ProgressPercentage;
                MultiExportStatus = progressInfo.CurrentOperation;
                
                var eta = progressInfo.EstimatedTimeRemaining;
                var etaText = eta.HasValue ? $" • ETA: {eta.Value:hh\\:mm\\:ss}" : "";
                
                var recoveryText = progressInfo.RecoveredExports > 0 ? $" • Recovered: {progressInfo.RecoveredExports}" : "";
                var validationText = progressInfo.ValidationErrors > 0 ? $" • Validation Errors: {progressInfo.ValidationErrors}" : "";
                
                MultiExportDetails = $"Combination {progressInfo.CurrentCombination}/{progressInfo.TotalCombinations} • " +
                                   $"Success: {progressInfo.SuccessfulExports} • Failed: {progressInfo.FailedExports}{recoveryText}{validationText}{etaText}";
                
                StatusMessage = $"Processing: {progressInfo.ProgressPercentage:F1}% complete";
            });

            // Step 3: Execute enhanced multi-parameter export
            _enhancedLoggingService.StartStepTiming(operationType, "Multi-Parameter Processing");
            var result = await _multiParameterService.ProcessMultipleParametersAsync(
                multiRequest, 
                outputDir, 
                progress, 
                _cancellationTokenSource.Token);
            _enhancedLoggingService.EndStepTiming(operationType, "Multi-Parameter Processing");

            // Update final status
            MultiExportProgress = 100;
            StatusMessage = $"Enhanced multi-parameter export completed: {result.SuccessfulExports}/{result.TotalCombinations} files";
            MultiExportStatus = "Export completed";
            MultiExportDetails = $"Total time: {result.ProcessingTime:hh\\:mm\\:ss} • " +
                               $"Success rate: {result.SuccessRate:F1}%";

            // Calculate and log total operation time
            var operationTotalTime = DateTime.Now - operationStartTime;
            _enhancedLoggingService.LogMultiExport($"✅ Enhanced multi-parameter export completed: {result.SuccessfulExports}/{result.TotalCombinations} files successful");
            _enhancedLoggingService.LogOperationTotalTime(operationType, operationTotalTime, false);
            
            TradeDataEXP.App.LogMessage($"✅ Enhanced multi-parameter export completed: {result.SuccessfulExports}/{result.TotalCombinations} files successful");

            // Show results dialog
            var message = $"Enhanced Multi-Parameter Export Completed!\n\n" +
                         $"📊 Total combinations: {result.TotalCombinations}\n" +
                         $"✅ Successful files: {result.SuccessfulExports}\n" +
                         $"❌ Failed files: {result.FailedExports}\n";

            // Add recovery information if any files were recovered
            var recoveredCount = result.FileResults.Count(fr => fr.Message?.Contains("Recovered") == true);
            if (recoveredCount > 0)
            {
                message += $"🔄 Recovered files: {recoveredCount}\n";
            }

            message += $"📁 Output directory: {result.OutputDirectory}\n" +
                      $"⏱️ Processing time: {result.ProcessingTime:hh\\:mm\\:ss}\n" +
                      $"📈 Success rate: {result.SuccessRate:F1}%";

            if (result.FailedExports > 0)
            {
                message += "\n\n⚠️ Some files failed to export even after recovery attempts. Check the log for details.";
            }

            if (recoveredCount > 0)
            {
                message += $"\n\n🔄 {recoveredCount} files required recovery operations but were successfully created.";
            }

            MessageBox.Show(message, "Enhanced Multi-Parameter Export Results", MessageBoxButton.OK, 
                result.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (OperationCanceledException)
        {
            var cancelledTime = DateTime.Now - operationStartTime;
            StatusMessage = "Multi-parameter export was cancelled";
            MultiExportStatus = "Export cancelled";
            MultiExportDetails = "Operation cancelled by user";
            _enhancedLoggingService.LogMultiExport("⏹️ Enhanced multi-parameter export was cancelled by user");
            _enhancedLoggingService.LogOperationTotalTime(operationType, cancelledTime, false);
            TradeDataEXP.App.LogMessage("⏹️ Enhanced multi-parameter export was cancelled by user");
            MessageBox.Show("Multi-parameter export was cancelled.", "Export Cancelled", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var errorTime = DateTime.Now - operationStartTime;
            _enhancedLoggingService.LogError("Multi-parameter export operation failed", ex);
            _enhancedLoggingService.LogMultiExport($"❌ Enhanced multi-parameter export failed: {ex.Message}");
            _enhancedLoggingService.LogOperationTotalTime(operationType, errorTime, false);
            StatusMessage = $"Error: {ex.Message}";
            MultiExportStatus = "Export failed";
            MultiExportDetails = ex.Message;
            TradeDataEXP.App.LogMessage($"❌ Enhanced multi-parameter export failed: {ex.Message}");
            MessageBox.Show($"Error during enhanced multi-parameter export: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
            IsMultiExportInProgress = false;
            IsAnyOperationInProgress = false;
            CanCancelMultiExport = false;
            CanCancelOperation = false;
            CurrentOperationType = string.Empty;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelOperation()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            // There's an active operation to cancel
            TradeDataEXP.App.LogMessage($"⏹️ User requested cancellation of {CurrentOperationType} operation");
            _cancellationTokenSource.Cancel();
            
            if (!string.IsNullOrEmpty(CurrentOperationType))
            {
                StatusMessage = $"Cancelling {CurrentOperationType}...";
                
                if (IsMultiExportInProgress)
                {
                    MultiExportStatus = "Cancelling export...";
                    MultiExportDetails = "Please wait while current operations complete";
                }
            }
            else
            {
                StatusMessage = "Cancelling operation...";
            }
            
            TradeDataEXP.App.LogMessage($"🔄 Cancellation token triggered for {CurrentOperationType} operation");
        }
        else
        {
            // No operation is running - provide feedback
            StatusMessage = "No operation is currently running to cancel";
            TradeDataEXP.App.LogMessage("⚠️ Cancel requested but no operation is currently running");
            
            // Optional: Reset any stuck states
            if (IsAnyOperationInProgress || IsMultiExportInProgress || IsLoading)
            {
                TradeDataEXP.App.LogMessage("🔧 Resetting stuck operation states");
                IsLoading = false;
                IsMultiExportInProgress = false;
                IsAnyOperationInProgress = false;
                CanCancelMultiExport = false;
                CanCancelOperation = false;
                CurrentOperationType = string.Empty;
                MultiExportStatus = "";
                MultiExportDetails = "";
                StatusMessage = "Reset - Ready for new operation";
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelMultiExport))]
    private void CancelMultiExport()
    {
        CancelOperation(); // Use the unified cancellation method
    }

    private static string EstimateProcessingTime(int combinations)
    {
        // Rough estimate: ~2-5 seconds per combination depending on data size
        var estimatedSeconds = combinations * 3;
        var timeSpan = TimeSpan.FromSeconds(estimatedSeconds);
        
        if (timeSpan.TotalHours >= 1)
            return $"~{timeSpan.TotalHours:F1} hours";
        else if (timeSpan.TotalMinutes >= 1)
            return $"~{timeSpan.TotalMinutes:F0} minutes";
        else
            return $"~{timeSpan.TotalSeconds:F0} seconds";
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

        if (!string.IsNullOrWhiteSpace(Parameters.IndianPort) && Parameters.IndianPort != "%")
            parts.Add(Parameters.IndianPort.Replace(" ", "_").Replace("%", ""));

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
            dateRange = $"{GetMonthName(fromMonth)}{fromYear % 100:00}";
        }
        else
        {
            dateRange = $"{GetMonthName(fromMonth)}{fromYear % 100:00}-{GetMonthName(toMonth)}{toYear % 100:00}";
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

    /// <summary>
    /// Helper method to generate all combinations for logging purposes
    /// </summary>
    private IEnumerable<ParameterCombination> GenerateAllCombinationsForLogging(MultiParameterRequest request)
    {
        var hsCodeList = request.HsCodes;
        var productList = request.Products;
        var exporterList = request.Exporters;
        var portList = request.Ports;
        var iecCodeList = request.IecCodes;
        var foreignCountryList = request.ForeignCountries;
        var foreignPartyList = request.ForeignParties;

        // Generate cartesian product (all combinations)
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
}
