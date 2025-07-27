using System;

namespace TradeDataEXP.Models;

public class ExportData
{
    public string? SB_NO { get; set; }
    public string? HS4 { get; set; }
    public DateTime? SB_Date { get; set; }
    public string? HS_Code { get; set; }
    public string? Product { get; set; }
    public decimal? QTY { get; set; }
    public string? Unit { get; set; }
    public decimal? UnitRateInForeignCurrency { get; set; }
    public string? UnitRateCurrency { get; set; }
    public decimal? ValueInFC { get; set; }
    public decimal? TotalSBValueInINRInLacs { get; set; }
    public decimal? UnitRateINR { get; set; }
    public decimal? FOB_USD { get; set; }
    public decimal? Unit_Rate_USD { get; set; }
    public string? PortOfDestination { get; set; }
    public string? CtryOfDestination { get; set; }
    public string? PortOfOrigin { get; set; }
    public string? Ship_Mode { get; set; }
    public string? IEC { get; set; }
    public string? IndianExporterName { get; set; }
    public string? ExporterAdd1 { get; set; }
    public string? ExporterAdd2 { get; set; }
    public string? ExporterCity { get; set; }
    public string? Pin { get; set; }
    public string? ForeignImporterName { get; set; }
    public string? FOR_Add1 { get; set; }
    public string? Item_no { get; set; }
    public string? Invoice_no { get; set; }
    public string? DRAW_BACK { get; set; }
    public string? CHA_NO { get; set; }
    public string? CHA_NAME { get; set; }
    public decimal? std_qty { get; set; }
    public int? MonthSerial { get; set; }
}
