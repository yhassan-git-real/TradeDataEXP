using System;
using System.Linq;

namespace TradeDataEXP.Models;

/// <summary>
/// Represents a multi-parameter export request with comma-separated values
/// </summary>
public class MultiParameterRequest
{
    public string[] HsCodes { get; set; } = Array.Empty<string>();
    public string[] Products { get; set; } = Array.Empty<string>();
    public string[] Exporters { get; set; } = Array.Empty<string>();
    public string[] Ports { get; set; } = Array.Empty<string>();
    public string[] IecCodes { get; set; } = Array.Empty<string>();
    public string[] ForeignCountries { get; set; } = Array.Empty<string>();
    public string[] ForeignParties { get; set; } = Array.Empty<string>();
    public string FromMonthSerial { get; set; } = string.Empty;
    public string ToMonthSerial { get; set; } = string.Empty;
    
    public int TotalCombinations => HsCodes.Length * Products.Length * Exporters.Length * 
                                  Ports.Length * IecCodes.Length * ForeignCountries.Length * 
                                  ForeignParties.Length;
    
    public bool IsValid => !string.IsNullOrEmpty(FromMonthSerial) && !string.IsNullOrEmpty(ToMonthSerial);
    
    /// <summary>
    /// Creates MultiParameterRequest from ExportParameters with comma-separated values
    /// </summary>
    public static MultiParameterRequest FromExportParameters(ExportParameters parameters)
    {
        return new MultiParameterRequest
        {
            HsCodes = SplitParameter(parameters.HsCode),
            Products = SplitParameter(parameters.Product),
            Exporters = SplitParameter(parameters.ExporterName),
            Ports = SplitParameter(parameters.IndianPort),
            IecCodes = SplitParameter(parameters.Iec),
            ForeignCountries = SplitParameter(parameters.ForeignCountry),
            ForeignParties = SplitParameter(parameters.ForeignParty),
            FromMonthSerial = parameters.FromMonthSerial,
            ToMonthSerial = parameters.ToMonthSerial
        };
    }
    
    public static string[] SplitParameter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new[] { "%" };
            
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s))
                   .ToArray();
    }
}
