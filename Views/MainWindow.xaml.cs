using System;
using System.Windows;
using TradeDataEXP.ViewModels;

namespace TradeDataEXP.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                TradeDataEXP.App.LogMessage("MainWindow constructor starting...");
                
                TradeDataEXP.App.LogMessage("Calling InitializeComponent...");
                InitializeComponent();
                TradeDataEXP.App.LogMessage("InitializeComponent completed");
                
                TradeDataEXP.App.LogMessage("Creating MainViewModel with dependency injection...");
                DataContext = new MainViewModel(
                    TradeDataEXP.App.ConfigurationService,
                    TradeDataEXP.App.DatabaseService,
                    TradeDataEXP.App.ExcelExportService
                );
                TradeDataEXP.App.LogMessage("MainViewModel created and assigned to DataContext");
                
                TradeDataEXP.App.LogMessage("MainWindow constructor completed successfully");
                
                // Ensure window is visible and on top
                this.Visibility = Visibility.Visible;
                this.Show();
                this.Activate();
                this.Topmost = true;
                this.Topmost = false; // Reset topmost to allow normal behavior
                
                TradeDataEXP.App.LogMessage("MainWindow visibility ensured");
            }
            catch (Exception ex)
            {
                TradeDataEXP.App.LogMessage($"ERROR in MainWindow constructor: {ex}");
                MessageBox.Show($"Failed to create main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TradeDataEXP.App.LogMessage("Theme toggle clicked - basic implementation");
                MessageBox.Show("Theme toggle will be implemented with Material Design later", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TradeDataEXP.App.LogMessage($"ERROR toggling theme: {ex}");
                MessageBox.Show($"Failed to toggle theme: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}