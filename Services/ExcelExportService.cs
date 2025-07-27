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

            // Add headers
            var headers = new[]
            {
                "SB NO", "HS4", "SB Date", "HS Code", "Product", "QTY", "Unit",
                "Unit Rate (FC)", "Unit Rate Currency", "Value (FC)", "Total SB Value (INR Lacs)",
                "Unit Rate (INR)", "FOB USD", "Unit Rate USD", "Port of Destination",
                "Country of Destination", "Port of Origin", "Ship Mode", "IEC",
                "Indian Exporter Name", "Exporter Add1", "Exporter Add2", "Exporter City",
                "Pin", "Foreign Importer Name", "Foreign Add1", "Item No", "Invoice No",
                "Draw Back", "CHA No", "CHA Name", "Std Qty"
            };

            // Set headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Add data
            var dataList = data.ToList();
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                var excelRow = row + 2; // Start from row 2 (after header)

                worksheet.Cell(excelRow, 1).Value = item.SB_NO;
                worksheet.Cell(excelRow, 2).Value = item.HS4;
                worksheet.Cell(excelRow, 3).Value = item.SB_Date;
                worksheet.Cell(excelRow, 4).Value = item.HS_Code;
                worksheet.Cell(excelRow, 5).Value = item.Product;
                worksheet.Cell(excelRow, 6).Value = item.QTY;
                worksheet.Cell(excelRow, 7).Value = item.Unit;
                worksheet.Cell(excelRow, 8).Value = item.UnitRateInForeignCurrency;
                worksheet.Cell(excelRow, 9).Value = item.UnitRateCurrency;
                worksheet.Cell(excelRow, 10).Value = item.ValueInFC;
                worksheet.Cell(excelRow, 11).Value = item.TotalSBValueInINRInLacs;
                worksheet.Cell(excelRow, 12).Value = item.UnitRateINR;
                worksheet.Cell(excelRow, 13).Value = item.FOB_USD;
                worksheet.Cell(excelRow, 14).Value = item.Unit_Rate_USD;
                worksheet.Cell(excelRow, 15).Value = item.PortOfDestination;
                worksheet.Cell(excelRow, 16).Value = item.CtryOfDestination;
                worksheet.Cell(excelRow, 17).Value = item.PortOfOrigin;
                worksheet.Cell(excelRow, 18).Value = item.Ship_Mode;
                worksheet.Cell(excelRow, 19).Value = item.IEC;
                worksheet.Cell(excelRow, 20).Value = item.IndianExporterName;
                worksheet.Cell(excelRow, 21).Value = item.ExporterAdd1;
                worksheet.Cell(excelRow, 22).Value = item.ExporterAdd2;
                worksheet.Cell(excelRow, 23).Value = item.ExporterCity;
                worksheet.Cell(excelRow, 24).Value = item.Pin;
                worksheet.Cell(excelRow, 25).Value = item.ForeignImporterName;
                worksheet.Cell(excelRow, 26).Value = item.FOR_Add1;
                worksheet.Cell(excelRow, 27).Value = item.Item_no;
                worksheet.Cell(excelRow, 28).Value = item.Invoice_no;
                worksheet.Cell(excelRow, 29).Value = item.DRAW_BACK;
                worksheet.Cell(excelRow, 30).Value = item.CHA_NO;
                worksheet.Cell(excelRow, 31).Value = item.CHA_NAME;
                worksheet.Cell(excelRow, 32).Value = item.std_qty;

                // Apply alternating row colors
                if (row % 2 == 1)
                {
                    var range = worksheet.Range(excelRow, 1, excelRow, headers.Length);
                    range.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Apply borders
                var dataRange = worksheet.Range(excelRow, 1, excelRow, headers.Length);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Format date columns
            var dateColumn = worksheet.Column(3); // SB Date column
            var dateFormat = _configService.GetValue("EXCEL_DATE_FORMAT", "dd-mmm-yyyy");
            dateColumn.Style.DateFormat.Format = dateFormat;

            // Format number columns (right-align and add commas)
            var numberColumns = new[] { 6, 8, 10, 11, 12, 13, 14, 32 }; // QTY, rates, values
            var numberFormat = _configService.GetValue("EXCEL_NUMBER_FORMAT", "#,##0.00");
            foreach (var colIndex in numberColumns)
            {
                var column = worksheet.Column(colIndex);
                column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                column.Style.NumberFormat.Format = numberFormat;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Create output directory if it doesn't exist
            var outputDir = _configService.GetOutputDirectory();
            Directory.CreateDirectory(outputDir);

            var filePath = Path.Combine(outputDir, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            workbook.SaveAs(filePath);

            return filePath;
        });
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
            var worksheet = workbook.Worksheets.Add("Export Data");

            // Add headers
            var headers = new[]
            {
                "SB NO", "HS4", "SB Date", "HS Code", "Product", "QTY", "Unit",
                "Unit Rate (FC)", "Unit Rate Currency", "Value (FC)", "Total SB Value (INR Lacs)",
                "Unit Rate (INR)", "FOB USD", "Unit Rate USD", "Port of Destination",
                "Country of Destination", "Port of Origin", "Ship Mode", "IEC",
                "Indian Exporter Name", "Exporter Add1", "Exporter Add2", "Exporter City",
                "Pin", "Foreign Importer Name", "Foreign Add1", "Item No", "Invoice No",
                "Draw Back", "CHA No", "CHA Name", "Std Qty"
            };

            // Set headers
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Add data
            var dataList = data.ToList();
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                var rowIndex = row + 2; // Start from row 2 (after headers)

                worksheet.Cell(rowIndex, 1).Value = item.SB_NO;
                worksheet.Cell(rowIndex, 2).Value = item.HS4;
                worksheet.Cell(rowIndex, 3).Value = item.SB_Date;
                worksheet.Cell(rowIndex, 4).Value = item.HS_Code;
                worksheet.Cell(rowIndex, 5).Value = item.Product;
                worksheet.Cell(rowIndex, 6).Value = item.QTY;
                worksheet.Cell(rowIndex, 7).Value = item.Unit;
                worksheet.Cell(rowIndex, 8).Value = item.UnitRateInForeignCurrency;
                worksheet.Cell(rowIndex, 9).Value = item.UnitRateCurrency;
                worksheet.Cell(rowIndex, 10).Value = item.ValueInFC;
                worksheet.Cell(rowIndex, 11).Value = item.TotalSBValueInINRInLacs;
                worksheet.Cell(rowIndex, 12).Value = item.UnitRateINR;
                worksheet.Cell(rowIndex, 13).Value = item.FOB_USD;
                worksheet.Cell(rowIndex, 14).Value = item.Unit_Rate_USD;
                worksheet.Cell(rowIndex, 15).Value = item.PortOfDestination;
                worksheet.Cell(rowIndex, 16).Value = item.CtryOfDestination;
                worksheet.Cell(rowIndex, 17).Value = item.PortOfOrigin;
                worksheet.Cell(rowIndex, 18).Value = item.Ship_Mode;
                worksheet.Cell(rowIndex, 19).Value = item.IEC;
                worksheet.Cell(rowIndex, 20).Value = item.IndianExporterName;
                worksheet.Cell(rowIndex, 21).Value = item.ExporterAdd1;
                worksheet.Cell(rowIndex, 22).Value = item.ExporterAdd2;
                worksheet.Cell(rowIndex, 23).Value = item.ExporterCity;
                worksheet.Cell(rowIndex, 24).Value = item.Pin;
                worksheet.Cell(rowIndex, 25).Value = item.ForeignImporterName;
                worksheet.Cell(rowIndex, 26).Value = item.FOR_Add1;
                worksheet.Cell(rowIndex, 27).Value = item.Item_no;
                worksheet.Cell(rowIndex, 28).Value = item.Invoice_no;
                worksheet.Cell(rowIndex, 29).Value = item.DRAW_BACK;
                worksheet.Cell(rowIndex, 30).Value = item.CHA_NO;
                worksheet.Cell(rowIndex, 31).Value = item.CHA_NAME;
                worksheet.Cell(rowIndex, 32).Value = item.std_qty;
            }

            // Format date column
            var dateColumn = worksheet.Column(3); // SB Date column
            dateColumn.Style.DateFormat.Format = "dd-mmm-yyyy";

            // Format number columns (right-align and add commas)
            var numberColumns = new[] { 6, 8, 10, 11, 12, 13, 14, 32 }; // QTY, rates, values
            foreach (var colIndex in numberColumns)
            {
                var column = worksheet.Column(colIndex);
                column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                column.Style.NumberFormat.Format = "#,##0.00";
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            workbook.SaveAs(filePath);
        });
    }

    public string FormatCurrency(decimal value)
    {
        return value.ToString("C2"); // Currency format with 2 decimal places
    }

    public string GenerateFileName()
    {
        return $"TradeData_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
    }
}
