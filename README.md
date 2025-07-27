# TradeDataEXP - Modern Export Trade Data Application

A modern C# WPF application built with .NET 8.0 that replaces the legacy VB6 export trade data system. This application provides a clean, responsive interface for querying and exporting trade data with advanced filtering capabilities.

## 🚀 Features

- **Modern UI**: Built with Material Design for WPF
- **Light/Dark Theme**: Toggle between light and dark modes
- **Advanced Filtering**: Filter by HS Code, Product, Exporter, IEC, Foreign Party, Country, Port, and date range
- **Data Preview**: Preview top 100 records before full export
- **Excel Export**: Export to beautifully formatted Excel files with auto-sizing and styling
- **Responsive Design**: Optimized for various screen sizes
- **MVVM Architecture**: Clean separation of concerns with proper data binding

## 🛠️ Technology Stack

- **.NET 8.0** - Latest .NET framework
- **WPF** - Windows Presentation Foundation for UI
- **MaterialDesignInXAML** - Modern Material Design theme
- **Dapper** - Lightweight ORM for database access
- **ClosedXML** - Excel file generation
- **CommunityToolkit.Mvvm** - MVVM helpers and commands
- **SQL Server** - Database backend

## 📋 Prerequisites

- Windows 10/11
- .NET 8.0 Runtime
- SQL Server access to `MATRIX` server with `Raw_Process` database
- Visual Studio 2022 (for development)

## 🔧 Installation & Setup

### For End Users
1. Download the latest release from the releases page
2. Run `TradeDataEXP.exe`
3. Ensure you have access to the SQL Server database

### For Developers
1. Clone the repository
2. Open `TradeDataEXP.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Update connection string in `App.config` if needed
5. Build and run the application

## 📊 Database Configuration

The application connects to:
- **Server**: MATRIX
- **Database**: Raw_Process  
- **Authentication**: SQL Server (module/tcs@2015)

Update the connection string in `App.config` if your setup differs:

```xml
<connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Server=YOUR_SERVER;Database=Raw_Process;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;" 
         providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

## 🎯 Usage

### Basic Workflow
1. **Enter Parameters**: Fill in the desired filter criteria
   - HS Code: Use wildcards (e.g., `3901%`) or leave blank for all
   - Product: Product name or partial name
   - Exporter Name: Company name
   - IEC: Import Export Code
   - Foreign Party: Foreign importer name
   - Foreign Country: Destination country
   - Port: Port of origin
   - Date Range: From/To month in YYYYMM format (e.g., 202401 to 202406)

2. **Preview Data**: Click "Preview" to see the first 100 records
3. **Export to Excel**: Click "Download Excel" to export all matching records
4. **Clear**: Reset all fields to start over

### Input Format Examples
- **HS Code**: `3901`, `3901%`, `39` (wildcards supported)
- **MonthSerial**: `202401` (January 2024), `202412` (December 2024)
- **Text Fields**: Support partial matching with wildcards

### Excel Export Features
- **Professional Formatting**: Bold headers with colored background
- **Auto-sizing**: Columns automatically adjust to content
- **Alternating Rows**: Better readability with alternating row colors
- **Date Formatting**: Proper date format (dd-MMM-yyyy)
- **Number Formatting**: Comma separators and right alignment for numeric values
- **File Naming**: Intelligent naming based on selected criteria

## 🔍 Data Sources

The application works with the following database objects:
- **Stored Procedure**: `[dbo].[ExportData_New1]` - Processes and filters data
- **View**: `[dbo].[EXPDATA]` - Main data source for export records
- **Table**: `EXPTEMP` - Temporary table populated by the stored procedure

## 🎨 UI Features

### Theme Support
- **Light Theme**: Default professional appearance
- **Dark Theme**: Easy on the eyes for extended use
- **Toggle Button**: Top-right corner for instant theme switching

### Responsive Design
- **Minimum Size**: 1000x600 pixels
- **Optimized Layout**: Adapts to different screen sizes
- **Touch Friendly**: Large buttons and input areas

### Status Indicators
- **Progress Bar**: Shows operation progress
- **Status Messages**: Real-time feedback on operations
- **Record Count**: Displays number of records in preview

## 📁 Project Structure

```
TradeDataEXP/
├── Models/
│   ├── ExportData.cs          # Data model for export records
│   └── ExportParameters.cs    # Input parameters model
├── Services/
│   ├── DatabaseService.cs     # Database operations
│   └── ExcelExportService.cs  # Excel generation
├── ViewModels/
│   └── MainViewModel.cs       # Main application logic
├── Views/
│   └── MainWindow.xaml        # Main UI
├── Converters/
│   └── InverseBooleanConverter.cs
└── App.xaml                   # Application resources
```

## 🚀 Building for Production

### Self-Contained Deployment
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Framework-Dependent Deployment
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## 🔧 Configuration Options

### App.config Settings
```xml
<appSettings>
    <add key="ExcelExportPath" value="~/Desktop/TradeDataEXP_Exports" />
    <add key="CommandTimeout" value="300" />
</appSettings>
```

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Failed**
   - Check SQL Server connection
   - Verify credentials in App.config
   - Ensure network connectivity to MATRIX server

2. **Excel Export Fails**
   - Check disk space
   - Verify write permissions to export directory
   - Close any open Excel files with similar names

3. **Application Won't Start**
   - Ensure .NET 8.0 Runtime is installed
   - Check for missing dependencies
   - Run as administrator if needed

### Performance Tips
- Use specific date ranges to limit data volume
- Apply filters to reduce result set size
- Close preview before large Excel exports

## 📞 Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the application logs
3. Contact the development team

## 🔄 Version History

### v1.0.0 (Initial Release)
- Complete rewrite from VB6 to C# WPF
- Modern Material Design UI
- Enhanced Excel export capabilities
- Improved performance and reliability
- Light/Dark theme support

---

**Note**: This application replaces the legacy VB6 `EXP_Module.vbp` system with modern technology while maintaining compatibility with existing database structures and business logic.
