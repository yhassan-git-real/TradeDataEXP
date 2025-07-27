using System;
using System.Collections.Generic;
using System.IO;

namespace TradeDataEXP.Services
{
    public interface IConfigurationService
    {
        string GetConnectionString();
        string GetValue(string key);
        string GetValue(string key, string defaultValue);
        T GetValue<T>(string key);
        T GetValue<T>(string key, T defaultValue);
        string GetLogFilePath();
        string GetOutputDirectory();
        string GetStoredProcedureName();
        string GetViewName();
        int GetQueryTimeout();
        int GetQueryTopLimit();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, string> _config;
        private static readonly string DefaultConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

        public ConfigurationService() : this(DefaultConfigPath)
        {
        }

        public ConfigurationService(string configPath)
        {
            _config = new Dictionary<string, string>();
            LoadConfiguration(configPath);
        }

        private void LoadConfiguration(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    // Configuration file not found, using default values
                    LoadDefaultConfiguration();
                    return;
                }

                var lines = File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    var equalIndex = trimmedLine.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        var key = trimmedLine.Substring(0, equalIndex).Trim();
                        var value = trimmedLine.Substring(equalIndex + 1).Trim();
                        
                        // Remove quotes if present
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        
                        _config[key] = value;
                    }
                }

                // Configuration loaded successfully
            }
            catch (Exception ex)
            {
                // Error loading configuration, using defaults
                LoadDefaultConfiguration();
            }
        }

        private void LoadDefaultConfiguration()
        {
            // Fallback to hardcoded values if .env file is not available
            _config["DB_SERVER"] = "MATRIX";
            _config["DB_NAME"] = "Raw_Process";
            _config["DB_USER"] = "module";
            _config["DB_PASSWORD"] = "tcs@2015";
            _config["DB_TRUST_SERVER_CERTIFICATE"] = "true";
            _config["DB_CONNECTION_TIMEOUT"] = "30";
            _config["DB_VIEW_NAME"] = "EXPDATA";
            _config["DB_SCHEMA"] = "dbo";
            _config["STORED_PROCEDURE_NAME"] = "ExportData_New1";
            _config["LOG_DIRECTORY"] = @"I:\TradeDataHub\TradeDataEXP\Logs";
            _config["LOG_FILENAME_BASE"] = "TradeDataEXP_Log";
            _config["OUTPUT_DIRECTORY"] = @"Desktop\TradeDataEXP_Exports";
            _config["OUTPUT_USE_DESKTOP"] = "true";
            _config["QUERY_TOP_LIMIT"] = "1000";
            _config["QUERY_TIMEOUT"] = "300";
            
            // Default configuration values loaded
        }

        public string GetValue(string key)
        {
            return _config.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string GetValue(string key, string defaultValue)
        {
            return _config.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public T GetValue<T>(string key)
        {
            return GetValue<T>(key, default(T)!);
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            if (!_config.TryGetValue(key, out var value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public string GetConnectionString()
        {
            var server = GetValue("DB_SERVER");
            var database = GetValue("DB_NAME");
            var user = GetValue("DB_USER");
            var password = GetValue("DB_PASSWORD");
            var trustCert = GetValue("DB_TRUST_SERVER_CERTIFICATE", "true");
            var timeout = GetValue("DB_CONNECTION_TIMEOUT", "30");

            return $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate={trustCert};Connection Timeout={timeout};";
        }

        public string GetLogFilePath()
        {
            var logDir = GetValue("LOG_DIRECTORY");
            var baseLogFile = GetValue("LOG_FILENAME_BASE", "TradeDataEXP_Log");
            
            // Add current date to the filename (format: YYYYMMDD)
            var dateStamp = DateTime.Now.ToString("yyyyMMdd");
            var logFileWithDate = $"{baseLogFile}_{dateStamp}.txt";
            
            return Path.Combine(logDir, logFileWithDate);
        }

        public string GetOutputDirectory()
        {
            var outputDir = GetValue("OUTPUT_DIRECTORY", @"Desktop\TradeDataEXP_Exports");
            var useDesktop = GetValue<bool>("OUTPUT_USE_DESKTOP", true);
            
            if (useDesktop && outputDir.StartsWith("Desktop"))
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var relativePath = outputDir.Substring("Desktop".Length).TrimStart('\\', '/');
                return Path.Combine(desktopPath, relativePath);
            }
            
            return outputDir;
        }

        public string GetStoredProcedureName()
        {
            return GetValue("STORED_PROCEDURE_NAME", "ExportData_New1");
        }

        public string GetViewName()
        {
            var schema = GetValue("DB_SCHEMA", "dbo");
            var viewName = GetValue("DB_VIEW_NAME", "EXPDATA");
            return $"[{schema}].[{viewName}]";
        }

        public int GetQueryTimeout()
        {
            return GetValue<int>("QUERY_TIMEOUT", 300);
        }

        public int GetQueryTopLimit()
        {
            return GetValue<int>("QUERY_TOP_LIMIT", 1000);
        }
    }
}
