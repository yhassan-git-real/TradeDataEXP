using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services;

public interface IExcelExportService
{
    Task<string> ExportToExcelAsync(IEnumerable<ExportData> data, string fileName);
    Task ExportToFileAsync(IEnumerable<ExportData> data, string filePath);
    string FormatCurrency(decimal value);
    string GenerateFileName();
}

public class ExcelExportService : IExcelExportService
{
    private readonly IConfigurationService _configService;

    public ExcelExportService(IConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    public async Task<string> ExportToExcelAsync(IEnumerable<ExportData> data, string fileName)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheetName = _configService.GetValue("EXCEL_WORKSHEET_NAME", "Export Data");
            var worksheet = workbook.Worksheets.Add(worksheetName);

            var dataList = data.ToList();
            if (!dataList.Any())
            {
                worksheet.Cell(1, 1).Value = "No data available for export";
                var emptyOutputDir = _configService.GetOutputDirectory();
                Directory.CreateDirectory(emptyOutputDir);
                var emptyFilePath = Path.Combine(emptyOutputDir, $"{fileName}.xlsx");
                workbook.SaveAs(emptyFilePath);
                return emptyFilePath;
            }

            var firstRecord = dataList.First();
            var columnNames = firstRecord.GetColumnNames().ToList();

            for (int i = 0; i < columnNames.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = FormatColumnHeader(columnNames[i]);
                
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.Bold = true;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                cell.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Accent1, 0.5);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = false;
            }

            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                var excelRow = row + 2;

                for (int col = 0; col < columnNames.Count; col++)
                {
                    var columnName = columnNames[col];
                    var cellValue = item.GetValue<object>(columnName);
                    
                    var excelCell = worksheet.Cell(excelRow, col + 1);
                    SetCellValue(excelCell, cellValue, columnName);
                }

                var dataRange = worksheet.Range(excelRow, 1, excelRow, columnNames.Count);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();
            
            var allDataRange = worksheet.Range(1, 1, dataList.Count + 1, columnNames.Count);
            allDataRange.Style.Alignment.WrapText = false;

            var outputDir = _configService.GetOutputDirectory();
            Directory.CreateDirectory(outputDir);

            var filePath = Path.Combine(outputDir, $"{fileName}.xlsx");
            workbook.SaveAs(filePath);

            return filePath;
        });
    }

    /// <summary>
    /// Formats database column names to user-friendly headers using naming conventions
    /// This method automatically converts any column name to a readable format
    /// </summary>
    private string FormatColumnHeader(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return columnName;

        return FormatColumnNameToFriendlyText(columnName);
    }

    /// <summary>
    /// Converts database column names to friendly display text using intelligent formatting rules
    /// </summary>
    private string FormatColumnNameToFriendlyText(string columnName)
    {
        var formatted = columnName
            .Replace("_", " ")                    // Replace underscores with spaces
            .Replace("Ctry", "Country")           // Common abbreviation
            .Replace("No", "Number")              // Expand common abbreviations
            .Replace("Qty", "Quantity")
            .Replace("Ind", "Indian")
            .Replace("Sb", "SB")
            .Replace("Hs", "HS")
            .Replace("Iec", "IEC")
            .Replace("Fob", "FOB")
            .Replace("Fc", "Foreign Currency")
            .Replace("Inr", "INR")
            .Replace("Usd", "USD");

        // Convert to Title Case (first letter of each word capitalized)
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());
    }

    /// <summary>
    /// Sets cell value with appropriate formatting based on data type
    /// </summary>
    private void SetCellValue(IXLCell cell, object? value, string columnName)
    {
        if (value == null || value == DBNull.Value)
        {
            cell.Value = "";
            return;
        }

        cell.Style.Font.FontName = "Times New Roman";
        cell.Style.Font.FontSize = 10;
        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        cell.Style.Alignment.WrapText = false;

        switch (value)
        {
            case DateTime dateValue:
                cell.Value = dateValue;
                cell.Style.DateFormat.Format = "dd-mmm-yy";
                break;
                
            case decimal decimalValue:
            case double doubleValue:
            case float floatValue:
                cell.Value = Convert.ToDecimal(value);
                if (columnName.Contains("Rate") || columnName.Contains("Value") || 
                    columnName.Contains("FOB") || columnName.Contains("QTY"))
                {
                    var numberFormat = _configService.GetValue("EXCEL_NUMBER_FORMAT", "#,##0.00");
                    cell.Style.NumberFormat.Format = numberFormat;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                break;
                
            case int intValue:
            case long longValue:
                cell.Value = Convert.ToInt64(value);
                if (columnName.Contains("Serial") || columnName.Contains("MonthSerial"))
                {
                    cell.Style.NumberFormat.Format = "0";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                break;
                
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    public async Task ExportToFileAsync(IEnumerable<ExportData> data, string filePath)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheetName = _configService.GetValue("EXCEL_WORKSHEET_NAME", "Export Data");
            var worksheet = workbook.Worksheets.Add(worksheetName);

            var dataList = data.ToList();
            if (!dataList.Any())
            {
                worksheet.Cell(1, 1).Value = "No data available for export";
                workbook.SaveAs(filePath);
                return;
            }

            var firstRecord = dataList.First();
            var columnNames = firstRecord.GetColumnNames().ToList();

            for (int i = 0; i < columnNames.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = FormatColumnHeader(columnNames[i]);
                
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.Bold = true;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                cell.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Accent1, 0.5);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = false;
            }

            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                var excelRow = row + 2;

                for (int col = 0; col < columnNames.Count; col++)
                {
                    var columnName = columnNames[col];
                    var cellValue = item.GetValue<object>(columnName);
                    
                    var excelCell = worksheet.Cell(excelRow, col + 1);
                    SetCellValue(excelCell, cellValue, columnName);
                }

                var dataRange = worksheet.Range(excelRow, 1, excelRow, columnNames.Count);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            worksheet.Columns().AdjustToContents();
            
            var allDataRange = worksheet.Range(1, 1, dataList.Count + 1, columnNames.Count);
            allDataRange.Style.Alignment.WrapText = false;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            workbook.SaveAs(filePath);
        });
    }

    public string FormatCurrency(decimal value)
    {
        return value.ToString("C2");
    }

    /// <summary>
    /// Generates filename using standard pattern based on filters
    /// </summary>
    public string GenerateFileName()
    {
        var currentDate = DateTime.Now;
        var monthAbbr = GetMonthAbbreviation(currentDate.Month);
        var year = currentDate.ToString("yy");
        
        var fileName = new List<string>();
        
        var hsCode = _configService.GetValue("FILENAME_HSCODE", "");
        var product = _configService.GetValue("FILENAME_PRODUCT", "");
        var iec = _configService.GetValue("FILENAME_IEC", "");
        var exporter = _configService.GetValue("FILENAME_EXPORTER", "");
        var country = _configService.GetValue("FILENAME_COUNTRY", "");
        var forName = _configService.GetValue("FILENAME_FORNAME", "");
        var port = _configService.GetValue("FILENAME_PORT", "");
        
        if (!string.IsNullOrEmpty(hsCode) && hsCode != "%") 
            fileName.Add(CleanForFilename(hsCode));
        if (!string.IsNullOrEmpty(product) && product != "%") 
            fileName.Add(CleanForFilename(product));
        if (!string.IsNullOrEmpty(iec) && iec != "%") 
            fileName.Add(CleanForFilename(iec));
        if (!string.IsNullOrEmpty(exporter) && exporter != "%") 
            fileName.Add(CleanForFilename(exporter));
        if (!string.IsNullOrEmpty(country) && country != "%") 
            fileName.Add(CleanForFilename(country));
        if (!string.IsNullOrEmpty(forName) && forName != "%") 
            fileName.Add(CleanForFilename(forName));
        if (!string.IsNullOrEmpty(port) && port != "%") 
            fileName.Add(CleanForFilename(port));
        
        var baseFileName = fileName.Any() ? string.Join("_", fileName) : "TradeData";
        return $"{baseFileName}_{monthAbbr}{year}EXP.xlsx";
    }
    
    /// <summary>
    /// Cleans filename component for valid file names
    /// </summary>
    private string CleanForFilename(string text)
    {
        return text.Replace(" ", "_").Replace("&", "and").Replace("/", "_");
    }

    /// <summary>
    /// Gets month abbreviation in UPPERCASE format
    /// </summary>
    private string GetMonthAbbreviation(int month)
    {
        return month switch
        {
            1 => "JAN",
            2 => "FEB", 
            3 => "MAR",
            4 => "APR",
            5 => "MAY",
            6 => "JUN",
            7 => "JUL",
            8 => "AUG",
            9 => "SEP",
            10 => "OCT",
            11 => "NOV",
            12 => "DEC",
            _ => "UNK"
        };
    }
}
