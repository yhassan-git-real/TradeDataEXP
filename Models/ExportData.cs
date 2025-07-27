using System;
using System.Collections.Generic;
using System.Dynamic;

namespace TradeDataEXP.Models;

/// <summary>
/// Dynamic data model that adapts to the database view schema.
/// This avoids hardcoding column names and allows the database view to change
/// without requiring code changes.
/// </summary>
public class ExportData : DynamicObject
{
    private readonly Dictionary<string, object?> _data = new();

    /// <summary>
    /// Gets or sets values dynamically based on database column names
    /// </summary>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return _data.TryGetValue(binder.Name, out result);
    }

    /// <summary>
    /// Sets values dynamically based on database column names
    /// </summary>
    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        _data[binder.Name] = value;
        return true;
    }

    /// <summary>
    /// Gets a value by column name with type casting
    /// </summary>
    public T? GetValue<T>(string columnName)
    {
        if (!_data.TryGetValue(columnName, out var value) || value == null)
            return default(T);

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value.ToString()!;
            
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// Sets a value by column name
    /// </summary>
    public void SetValue(string columnName, object? value)
    {
        _data[columnName] = value;
    }

    /// <summary>
    /// Gets a value by column name as string (safe conversion)
    /// </summary>
    public string? GetString(string columnName)
    {
        return GetValue<string>(columnName);
    }

    /// <summary>
    /// Gets a value by column name as decimal (safe conversion)
    /// </summary>
    public decimal? GetDecimal(string columnName)
    {
        return GetValue<decimal?>(columnName);
    }

    /// <summary>
    /// Gets a value by column name as DateTime (safe conversion)
    /// </summary>
    public DateTime? GetDateTime(string columnName)
    {
        return GetValue<DateTime?>(columnName);
    }

    /// <summary>
    /// Gets a value by column name as integer (safe conversion)
    /// </summary>
    public int? GetInt(string columnName)
    {
        return GetValue<int?>(columnName);
    }

    /// <summary>
    /// Checks if a column exists in the data
    /// </summary>
    public bool HasColumn(string columnName)
    {
        return _data.ContainsKey(columnName);
    }

    /// <summary>
    /// Gets all column names available in this record
    /// </summary>
    public IEnumerable<string> GetColumnNames()
    {
        return _data.Keys;
    }

    /// <summary>
    /// Gets all data as a dictionary for serialization or debugging
    /// </summary>
    public Dictionary<string, object?> GetAllData()
    {
        return new Dictionary<string, object?>(_data);
    }

    /// <summary>
    /// Creates an ExportData instance from a dictionary (useful for database mapping)
    /// </summary>
    public static ExportData FromDictionary(IDictionary<string, object?> data)
    {
        var exportData = new ExportData();
        foreach (var kvp in data)
        {
            exportData.SetValue(kvp.Key, kvp.Value);
        }
        return exportData;
    }
}
