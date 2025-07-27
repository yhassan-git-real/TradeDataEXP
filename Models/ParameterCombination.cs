using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeDataEXP.Models;

/// <summary>
/// Represents a single parameter combination for processing
/// </summary>
public class ParameterCombination
{
    public string HsCode { get; set; } = "%";
    public string Product { get; set; } = "%";
    public string Exporter { get; set; } = "%";
    public string IndianPort { get; set; } = "%";
    public string IecCode { get; set; } = "%";
    public string ForeignCountry { get; set; } = "%";
    public string ForeignParty { get; set; } = "%";
    public string FromMonth { get; set; } = string.Empty;
    public string ToMonth { get; set; } = string.Empty;
    
    /// <summary>
    /// Converts this combination to ExportParameters for database queries
    /// </summary>
    public ExportParameters ToExportParameters()
    {
        return new ExportParameters
        {
            HsCode = HsCode,
            Product = Product,
            ExporterName = Exporter,
            IndianPort = IndianPort,
            Iec = IecCode,
            ForeignCountry = ForeignCountry,
            ForeignParty = ForeignParty,
            FromMonthSerial = FromMonth,
            ToMonthSerial = ToMonth
        };
    }
    
    /// <summary>
    /// Generates filename following standard pattern: "10_India_JAN25-JUL25EXP"
    /// </summary>
    public string GenerateFileName()
    {
        var parts = new List<string>();
        
        if (HsCode != "%") parts.Add(CleanForFilename(HsCode));
        if (Product != "%") parts.Add(CleanForFilename(Product));
        if (Exporter != "%") parts.Add(CleanForFilename(Exporter));
        if (IndianPort != "%") parts.Add(CleanForFilename(IndianPort));
        if (IecCode != "%") parts.Add(CleanForFilename(IecCode));
        if (ForeignCountry != "%") parts.Add(CleanForFilename(ForeignCountry));
        if (ForeignParty != "%") parts.Add(CleanForFilename(ForeignParty));
        
        var dateRange = GenerateDateRange();
        if (!string.IsNullOrEmpty(dateRange))
            parts.Add(dateRange);
            
        parts.Add("EXP");
        
        var baseFileName = parts.Any() ? string.Join("_", parts) : "TradeData_EXP";
        return baseFileName;
    }
    
    private string CleanForFilename(string text)
    {
        return text.Replace(" ", "_").Replace("&", "and").Replace("/", "_");
    }
    
    private string GenerateDateRange()
    {
        if (string.IsNullOrEmpty(FromMonth) || string.IsNullOrEmpty(ToMonth))
            return string.Empty;
            
        if (!int.TryParse(FromMonth, out int fromSerial) || !int.TryParse(ToMonth, out int toSerial))
            return string.Empty;
            
        var fromYear = fromSerial / 100;
        var fromMonthNum = fromSerial % 100;
        var toYear = toSerial / 100;
        var toMonthNum = toSerial % 100;
        
        var fromMonthAbbr = GetMonthAbbreviation(fromMonthNum);
        var toMonthAbbr = GetMonthAbbreviation(toMonthNum);
        
        if (fromSerial == toSerial)
        {
            return $"{fromMonthAbbr}{fromYear % 100:00}";
        }
        else
        {
            return $"{fromMonthAbbr}{fromYear % 100:00}-{toMonthAbbr}{toYear % 100:00}";
        }
    }
    
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
    
    /// <summary>
    /// Gets display text for progress reporting
    /// </summary>
    public string GetDisplayText()
    {
        var parts = new List<string>();
        
        if (HsCode != "%") parts.Add($"HS:{HsCode}");
        if (Product != "%") parts.Add($"Product:{Product}");
        if (Exporter != "%") parts.Add($"Exporter:{Exporter}");
        if (IndianPort != "%") parts.Add($"IndianPort:{IndianPort}");
        if (IecCode != "%") parts.Add($"IEC:{IecCode}");
        if (ForeignCountry != "%") parts.Add($"Country:{ForeignCountry}");
        if (ForeignParty != "%") parts.Add($"Party:{ForeignParty}");
        
        return parts.Any() ? string.Join(", ", parts) : "All parameters";
    }
}
