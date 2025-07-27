using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services;

public interface IDatabaseService
{
    Task ExecuteExportDataStoredProcedureAsync(ExportParameters parameters);
    Task<IEnumerable<ExportData>> GetExportDataAsync(ExportParameters parameters);
    Task<IEnumerable<ExportData>> GetExportDataAsync(int? maxRows = null);
    string GetConnectionString();
    string BuildWhereClause(ExportParameters parameters);
    string SanitizeInput(string input);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly IConfigurationService _configService;

    public DatabaseService(IConfigurationService configService)
    {
        try
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            TradeDataEXP.App.LogMessage("DatabaseService constructor starting...");
            
            _connectionString = _configService.GetConnectionString();
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string is null or empty");
            }
            
            var maskedConnectionString = _connectionString.Replace(_configService.GetValue("DB_PASSWORD"), "***");
            TradeDataEXP.App.LogMessage($"Connection string configured: {maskedConnectionString}");
            TradeDataEXP.App.LogMessage("DatabaseService constructor completed");
        }
        catch (Exception ex)
        {
            TradeDataEXP.App.LogMessage($"ERROR in DatabaseService constructor: {ex}");
            throw;
        }
    }

    public async Task ExecuteExportDataStoredProcedureAsync(ExportParameters parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var storedProcedure = _configService.GetStoredProcedureName();
        
        var procParameters = new
        {
            fromMonth = parameters.FromMonthSerial,
            ToMonth = parameters.ToMonthSerial,
            hs = string.IsNullOrWhiteSpace(parameters.HsCode) ? "%" : parameters.HsCode,
            prod = string.IsNullOrWhiteSpace(parameters.Product) ? "%" : parameters.Product,
            Iec = string.IsNullOrWhiteSpace(parameters.Iec) ? "%" : parameters.Iec,
            ExpCmp = string.IsNullOrWhiteSpace(parameters.ExporterName) ? "%" : parameters.ExporterName,
            forcount = string.IsNullOrWhiteSpace(parameters.ForeignCountry) ? "%" : parameters.ForeignCountry,
            forname = string.IsNullOrWhiteSpace(parameters.ForeignParty) ? "%" : parameters.ForeignParty,
            port = string.IsNullOrWhiteSpace(parameters.Port) ? "%" : parameters.Port
        };

        await connection.ExecuteAsync(storedProcedure, procParameters, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: 300);
    }

    public async Task<IEnumerable<ExportData>> GetExportDataAsync(int? maxRows = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var topLimit = maxRows ?? _configService.GetQueryTopLimit();
        var viewName = _configService.GetViewName();
        var timeout = _configService.GetQueryTimeout();

        var query = $@"
            SELECT TOP {topLimit} *
            FROM {viewName}
            ORDER BY [SB_Date] DESC";

        return await connection.QueryAsync<ExportData>(query, commandTimeout: timeout);
    }

    public async Task<IEnumerable<ExportData>> GetExportDataAsync(ExportParameters parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(parameters);
        var topLimit = _configService.GetQueryTopLimit();
        var viewName = _configService.GetViewName();
        var timeout = _configService.GetQueryTimeout();

        var query = $@"
            SELECT TOP {topLimit} *
            FROM {viewName}
            {whereClause}
            ORDER BY [SB_Date] DESC";

        return await connection.QueryAsync<ExportData>(query, commandTimeout: timeout);
    }    public string GetConnectionString()
    {
        return _connectionString;
    }

    public string BuildWhereClause(ExportParameters parameters)
    {
        var conditions = new List<string> { "1=1" }; // Base condition

        if (!string.IsNullOrWhiteSpace(parameters.HsCode))
        {
            conditions.Add($"[HS_Code] LIKE '{SanitizeInput(parameters.HsCode)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Product))
        {
            conditions.Add($"[Product] LIKE '%{SanitizeInput(parameters.Product)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.ExporterName))
        {
            conditions.Add($"[Indian Exporter Name] LIKE '%{SanitizeInput(parameters.ExporterName)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Iec))
        {
            conditions.Add($"[iec] LIKE '{SanitizeInput(parameters.Iec)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.ForeignParty))
        {
            conditions.Add($"[Foreign Importer Name] LIKE '%{SanitizeInput(parameters.ForeignParty)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.ForeignCountry))
        {
            conditions.Add($"[Ctry of Destination] LIKE '%{SanitizeInput(parameters.ForeignCountry)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Port))
        {
            conditions.Add($"[port of origin] LIKE '%{SanitizeInput(parameters.Port)}%'");
        }

        if (!string.IsNullOrWhiteSpace(parameters.FromMonthSerial))
        {
            if (int.TryParse(parameters.FromMonthSerial, out int fromMonth))
            {
                conditions.Add($"[MonthSerial] >= {fromMonth}");
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.ToMonthSerial))
        {
            if (int.TryParse(parameters.ToMonthSerial, out int toMonth))
            {
                conditions.Add($"[MonthSerial] <= {toMonth}");
            }
        }

        return "WHERE " + string.Join(" AND ", conditions);
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove potentially dangerous characters for SQL injection
        return input.Replace("'", "''")
                   .Replace("\"", "")
                   .Replace(";", "")
                   .Replace("--", "")
                   .Replace("/*", "")
                   .Replace("*/", "")
                   .Replace("xp_", "")
                   .Replace("sp_", "")
                   .Trim();
    }
}
