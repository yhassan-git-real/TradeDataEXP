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
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found at: {configPath}. Please ensure the .env file exists and contains all required settings.");
            }

            try
            {
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

                // Validate that all required configuration keys are present
                ValidateRequiredConfiguration();
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Error loading configuration from {configPath}: {ex.Message}", ex);
            }
        }

        private void ValidateRequiredConfiguration()
        {
            var requiredKeys = new[]
            {
                "DB_SERVER", "DB_NAME", "DB_USER", "DB_PASSWORD",
                "DB_VIEW_NAME", "DB_SCHEMA", "STORED_PROCEDURE_NAME",
                "LOG_DIRECTORY", "LOG_FILENAME_BASE", "OUTPUT_DIRECTORY",
                "QUERY_TOP_LIMIT", "QUERY_TIMEOUT"
            };

            var missingKeys = new List<string>();
            foreach (var key in requiredKeys)
            {
                if (!_config.ContainsKey(key) || string.IsNullOrWhiteSpace(_config[key]))
                {
                    missingKeys.Add(key);
                }
            }

            if (missingKeys.Count > 0)
            {
                throw new InvalidOperationException($"Missing required configuration keys in .env file: {string.Join(", ", missingKeys)}. Please ensure all required settings are defined in the .env file.");
            }
        }

        public string GetValue(string key)
        {
            if (!_config.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration key '{key}' is missing or empty in .env file. Please add this key with a valid value.");
            }
            return value;
        }

        public string GetValue(string key, string defaultValue)
        {
            return _config.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        public T GetValue<T>(string key)
        {
            var value = GetValue(key); // This will throw if key is missing
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert configuration value '{value}' for key '{key}' to type {typeof(T).Name}. Please check the value in .env file.", ex);
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            if (!_config.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert configuration value '{value}' for key '{key}' to type {typeof(T).Name}. Please check the value in .env file.", ex);
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
            var baseLogFile = GetValue("LOG_FILENAME_BASE");
            
            // Add current date to the filename (format: YYYYMMDD)
            var dateStamp = DateTime.Now.ToString("yyyyMMdd");
            var logFileWithDate = $"{baseLogFile}_{dateStamp}.txt";
            
            return Path.Combine(logDir, logFileWithDate);
        }

        public string GetOutputDirectory()
        {
            var outputDir = GetValue("OUTPUT_DIRECTORY");
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
            return GetValue("STORED_PROCEDURE_NAME");
        }

        public string GetViewName()
        {
            var schema = GetValue("DB_SCHEMA");
            var viewName = GetValue("DB_VIEW_NAME");
            return $"[{schema}].[{viewName}]";
        }

        public int GetQueryTimeout()
        {
            return GetValue<int>("QUERY_TIMEOUT");
        }

        public int GetQueryTopLimit()
        {
            return GetValue<int>("QUERY_TOP_LIMIT");
        }
    }
}
