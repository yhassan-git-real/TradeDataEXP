using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeDataEXP.Models;

public partial class ExportParameters : ObservableObject
{
    [ObservableProperty]
    private string hsCode = string.Empty;

    [ObservableProperty]
    private string product = string.Empty;

    [ObservableProperty]
    private string exporterName = string.Empty;

    [ObservableProperty]
    private string iec = string.Empty;

    [ObservableProperty]
    private string foreignParty = string.Empty;

    [ObservableProperty]
    private string foreignCountry = string.Empty;

    [ObservableProperty]
    private string indianPort = string.Empty;

    [ObservableProperty]
    private string fromMonthSerial = string.Empty;

    [ObservableProperty]
    private string toMonthSerial = string.Empty;

    public void Clear()
    {
        HsCode = string.Empty;
        Product = string.Empty;
        ExporterName = string.Empty;
        Iec = string.Empty;
        ForeignParty = string.Empty;
        ForeignCountry = string.Empty;
        IndianPort = string.Empty;
        FromMonthSerial = string.Empty;
        ToMonthSerial = string.Empty;
    }
}
