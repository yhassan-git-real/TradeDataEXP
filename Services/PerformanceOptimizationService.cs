using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeDataEXP.Models;

namespace TradeDataEXP.Services
{
    /// <summary>
    /// Performance monitoring service for tracking resource usage and optimization metrics
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Starts performance monitoring session
        /// </summary>
        void StartMonitoring(string sessionName);
        
        /// <summary>
        /// Stops performance monitoring and returns metrics
        /// </summary>
        PerformanceMetrics StopMonitoring();
        
        /// <summary>
        /// Records a database query operation
        /// </summary>
        void RecordDatabaseQuery(TimeSpan duration);
        
        /// <summary>
        /// Records a file I/O operation
        /// </summary>
        void RecordFileOperation(TimeSpan duration, long bytesWritten);
        
        /// <summary>
        /// Records an error or warning
        /// </summary>
        void RecordError(bool isError = true);
        
        /// <summary>
        /// Records a recovery attempt
        /// </summary>
        void RecordRecoveryAttempt();
        
        /// <summary>
        /// Updates concurrency metrics
        /// </summary>
        void UpdateConcurrency(int currentConcurrency);
        
        /// <summary>
        /// Triggers memory cleanup if needed
        /// </summary>
        Task<bool> CheckAndCleanupMemoryAsync();
        
        /// <summary>
        /// Gets current memory usage in MB
        /// </summary>
        long GetCurrentMemoryUsageMB();
    }

    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private PerformanceMetrics? _currentMetrics;
        private readonly OptimizationSettings _settings;
        private readonly ConcurrentQueue<int> _concurrencyHistory = new();
        private int _operationCount = 0;
        private readonly object _lockObject = new();

        public PerformanceMonitoringService(OptimizationSettings? settings = null)
        {
            _settings = settings ?? new OptimizationSettings();
        }

        public void StartMonitoring(string sessionName)
        {
            lock (_lockObject)
            {
                _currentMetrics = new PerformanceMetrics
                {
                    StartTime = DateTime.Now,
                    StartMemoryUsageMB = GetCurrentMemoryUsageMB()
                };
                
                _operationCount = 0;
                _concurrencyHistory.Clear();
                
                TradeDataEXP.App.LogMessage($"🚀 Performance monitoring started for session: {sessionName}");
            }
        }

        public PerformanceMetrics StopMonitoring()
        {
            lock (_lockObject)
            {
                if (_currentMetrics == null)
                    throw new InvalidOperationException("Monitoring session not started");

                _currentMetrics.EndTime = DateTime.Now;
                _currentMetrics.EndMemoryUsageMB = GetCurrentMemoryUsageMB();
                _currentMetrics.ProcessedCombinations = _operationCount;
                
                // Calculate average concurrency
                if (_concurrencyHistory.TryPeek(out _))
                {
                    var concurrencyValues = _concurrencyHistory.ToArray();
                    _currentMetrics.AverageConcurrency = concurrencyValues.Length > 0 
                        ? (int)concurrencyValues.Average() 
                        : 0;
                }

                var result = _currentMetrics;
                _currentMetrics = null;
                
                TradeDataEXP.App.LogMessage($"📊 Performance monitoring stopped. Efficiency: {result.GetEfficiencyRating():F1}%");
                TradeDataEXP.App.LogMessage(result.GetSummary());
                
                return result;
            }
        }

        public void RecordDatabaseQuery(TimeSpan duration)
        {
            if (_currentMetrics == null) return;
            
            lock (_lockObject)
            {
                _currentMetrics.DatabaseQueries++;
                _currentMetrics.TotalDatabaseTime = _currentMetrics.TotalDatabaseTime.Add(duration);
                
                if (duration.TotalMilliseconds > 5000) // Log slow queries
                {
                    TradeDataEXP.App.LogMessage($"⚠️ Slow database query detected: {duration.TotalMilliseconds:F0}ms");
                }
            }
        }

        public void RecordFileOperation(TimeSpan duration, long bytesWritten)
        {
            if (_currentMetrics == null) return;
            
            lock (_lockObject)
            {
                _currentMetrics.FilesCreated++;
                _currentMetrics.TotalBytesWritten += bytesWritten;
                _currentMetrics.TotalFileIOTime = _currentMetrics.TotalFileIOTime.Add(duration);
                
                _operationCount++;
            }
        }

        public void RecordError(bool isError = true)
        {
            if (_currentMetrics == null) return;
            
            lock (_lockObject)
            {
                if (isError)
                    _currentMetrics.Errors++;
                else
                    _currentMetrics.Warnings++;
            }
        }

        public void RecordRecoveryAttempt()
        {
            if (_currentMetrics == null) return;
            
            lock (_lockObject)
            {
                _currentMetrics.RecoveryAttempts++;
            }
        }

        public void UpdateConcurrency(int currentConcurrency)
        {
            if (_currentMetrics == null) return;
            
            _concurrencyHistory.Enqueue(currentConcurrency);
            
            // Keep only recent history (last 100 samples)
            while (_concurrencyHistory.Count > 100)
            {
                _concurrencyHistory.TryDequeue(out _);
            }
            
            lock (_lockObject)
            {
                _currentMetrics.MaxConcurrency = Math.Max(_currentMetrics.MaxConcurrency, currentConcurrency);
            }
        }

        public async Task<bool> CheckAndCleanupMemoryAsync()
        {
            var currentMemory = GetCurrentMemoryUsageMB();
            
            if (_currentMetrics != null)
            {
                lock (_lockObject)
                {
                    _currentMetrics.PeakMemoryUsageMB = Math.Max(_currentMetrics.PeakMemoryUsageMB, currentMemory);
                }
            }

            // Check if cleanup is needed
            if (currentMemory > _settings.MemorySettings.MaxMemoryUsageMB)
            {
                TradeDataEXP.App.LogMessage($"🧹 Memory cleanup triggered: {currentMemory}MB > {_settings.MemorySettings.MaxMemoryUsageMB}MB");
                
                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // Wait a bit for cleanup to take effect
                await Task.Delay(100);
                
                var afterMemory = GetCurrentMemoryUsageMB();
                var freed = currentMemory - afterMemory;
                
                TradeDataEXP.App.LogMessage($"🧹 Memory cleanup completed: freed {freed}MB, now using {afterMemory}MB");
                
                return freed > 0;
            }

            // Check if we should do regular cleanup
            if (_operationCount > 0 && _operationCount % _settings.MemorySettings.GCInterval == 0)
            {
                TradeDataEXP.App.LogMessage($"🧹 Regular memory cleanup at operation {_operationCount}");
                GC.Collect(0, GCCollectionMode.Optimized);
                return true;
            }

            return false;
        }

        public long GetCurrentMemoryUsageMB()
        {
            try
            {
                // Get working set memory (physical memory used by process)
                var process = Process.GetCurrentProcess();
                return process.WorkingSet64 / (1024 * 1024);
            }
            catch
            {
                // Fallback to GC memory if process info unavailable
                return GC.GetTotalMemory(false) / (1024 * 1024);
            }
        }
    }

    /// <summary>
    /// Smart caching service for database results with memory optimization
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearAsync();
        Task<long> GetMemoryUsageAsync();
        Task CompactAsync();
    }

    public class MemoryOptimizedCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly OptimizationSettings _settings;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);

        public MemoryOptimizedCacheService(OptimizationSettings settings)
        {
            _settings = settings;
            
            // Setup periodic cleanup
            _cleanupTimer = new Timer(async _ => await PerformCleanupAsync(), 
                null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiryTime > DateTime.Now)
                {
                    entry.LastAccessed = DateTime.Now;
                    entry.AccessCount++;
                    return entry.Value as T;
                }
                else
                {
                    // Expired - remove
                    _cache.TryRemove(key, out _);
                }
            }
            
            await Task.CompletedTask;
            return null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            var expiryTime = DateTime.Now.Add(expiry ?? TimeSpan.FromMinutes(_settings.CacheTimeoutMinutes));
            
            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                CreatedTime = DateTime.Now,
                LastAccessed = DateTime.Now,
                ExpiryTime = expiryTime,
                AccessCount = 1
            };

            _cache.AddOrUpdate(key, entry, (_, _) => entry);

            // Check if we need to cleanup
            if (_cache.Count > _settings.MemorySettings.MaxCacheSize)
            {
                _ = Task.Run(async () => await PerformCleanupAsync());
            }
            
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        public async Task ClearAsync()
        {
            _cache.Clear();
            await Task.CompletedTask;
        }

        public async Task<long> GetMemoryUsageAsync()
        {
            // Rough estimation of cache memory usage
            var estimatedSize = _cache.Count * 1024; // Assume 1KB per entry on average
            await Task.CompletedTask;
            return estimatedSize;
        }

        public async Task CompactAsync()
        {
            await PerformCleanupAsync(true);
        }

        private async Task PerformCleanupAsync(bool aggressive = false)
        {
            if (!await _cleanupSemaphore.WaitAsync(100))
                return; // Cleanup already in progress

            try
            {
                var now = DateTime.Now;
                var expiredKeys = new List<string>();
                var lruCandidates = new List<(string key, DateTime lastAccessed, int accessCount)>();

                // Find expired and LRU candidates
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiryTime <= now)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                    else
                    {
                        lruCandidates.Add((kvp.Key, kvp.Value.LastAccessed, kvp.Value.AccessCount));
                    }
                }

                // Remove expired entries
                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                // If still over limit or aggressive cleanup, remove LRU entries
                var targetSize = aggressive ? _settings.MemorySettings.MaxCacheSize / 2 : _settings.MemorySettings.MaxCacheSize;
                
                if (_cache.Count > targetSize)
                {
                    var toRemove = _cache.Count - targetSize;
                    var lruEntries = lruCandidates
                        .OrderBy(x => x.accessCount)
                        .ThenBy(x => x.lastAccessed)
                        .Take(toRemove)
                        .Select(x => x.key);

                    foreach (var key in lruEntries)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }

                TradeDataEXP.App.LogMessage($"🧹 Cache cleanup completed: {expiredKeys.Count} expired, {_cache.Count} remaining");
            }
            finally
            {
                _cleanupSemaphore.Release();
            }
        }

        private class CacheEntry
        {
            public string Key { get; set; } = string.Empty;
            public object Value { get; set; } = null!;
            public DateTime CreatedTime { get; set; }
            public DateTime LastAccessed { get; set; }
            public DateTime ExpiryTime { get; set; }
            public int AccessCount { get; set; }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _cleanupSemaphore?.Dispose();
        }
    }
}
