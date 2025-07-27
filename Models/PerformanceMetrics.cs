using System;
using System.Collections.Generic;

namespace TradeDataEXP.Models
{
    /// <summary>
    /// Performance metrics for monitoring export operations
    /// </summary>
    public class PerformanceMetrics
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration => EndTime - StartTime;
        
        // Memory metrics
        public long StartMemoryUsageMB { get; set; }
        public long PeakMemoryUsageMB { get; set; }
        public long EndMemoryUsageMB { get; set; }
        public long MemoryDeltaMB => EndMemoryUsageMB - StartMemoryUsageMB;
        
        // Processing metrics
        public int TotalCombinations { get; set; }
        public int ProcessedCombinations { get; set; }
        public double CombinationsPerSecond => TotalDuration.TotalSeconds > 0 ? ProcessedCombinations / TotalDuration.TotalSeconds : 0;
        
        // Database metrics
        public int DatabaseQueries { get; set; }
        public TimeSpan TotalDatabaseTime { get; set; }
        public double AverageDatabaseTimeMs => DatabaseQueries > 0 ? TotalDatabaseTime.TotalMilliseconds / DatabaseQueries : 0;
        
        // File I/O metrics
        public int FilesCreated { get; set; }
        public long TotalBytesWritten { get; set; }
        public TimeSpan TotalFileIOTime { get; set; }
        public double AverageFileIOTimeMs => FilesCreated > 0 ? TotalFileIOTime.TotalMilliseconds / FilesCreated : 0;
        
        // Threading metrics
        public int MaxConcurrency { get; set; }
        public int AverageConcurrency { get; set; }
        public TimeSpan TotalWaitTime { get; set; }
        
        // Error metrics
        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int RecoveryAttempts { get; set; }
        
        /// <summary>
        /// Gets a human-readable summary of performance metrics
        /// </summary>
        public string GetSummary()
        {
            var summary = $"🚀 Performance Summary:\n" +
                         $"⏱️ Duration: {TotalDuration:hh\\:mm\\:ss}\n" +
                         $"🎯 Throughput: {CombinationsPerSecond:F1} combinations/sec\n" +
                         $"🧠 Memory: {StartMemoryUsageMB}MB → {EndMemoryUsageMB}MB (Δ{MemoryDeltaMB:+0;-0;0}MB)\n" +
                         $"📊 Peak Memory: {PeakMemoryUsageMB}MB\n" +
                         $"💾 Database: {DatabaseQueries} queries, avg {AverageDatabaseTimeMs:F1}ms\n" +
                         $"📁 Files: {FilesCreated} created, {FormatBytes(TotalBytesWritten)}, avg {AverageFileIOTimeMs:F1}ms\n" +
                         $"🔀 Concurrency: max {MaxConcurrency}, avg {AverageConcurrency}\n";
                         
            if (Errors > 0 || Warnings > 0)
            {
                summary += $"⚠️ Issues: {Errors} errors, {Warnings} warnings, {RecoveryAttempts} recoveries\n";
            }
            
            return summary;
        }
        
        /// <summary>
        /// Gets performance efficiency rating (0-100)
        /// </summary>
        public double GetEfficiencyRating()
        {
            var baseScore = 100.0;
            
            // Penalize high memory usage (>500MB)
            if (PeakMemoryUsageMB > 500)
                baseScore -= Math.Min(20, (PeakMemoryUsageMB - 500) / 50.0);
            
            // Penalize slow throughput (<1 combination/sec)
            if (CombinationsPerSecond < 1.0)
                baseScore -= Math.Min(15, (1.0 - CombinationsPerSecond) * 10);
            
            // Penalize slow database queries (>1000ms average)
            if (AverageDatabaseTimeMs > 1000)
                baseScore -= Math.Min(15, (AverageDatabaseTimeMs - 1000) / 200.0);
            
            // Penalize errors
            baseScore -= Math.Min(25, Errors * 5);
            
            // Bonus for good concurrency utilization
            var idealConcurrency = Math.Min(Environment.ProcessorCount, 8);
            if (MaxConcurrency >= idealConcurrency * 0.8)
                baseScore += 5;
            
            return Math.Max(0, Math.Min(100, baseScore));
        }
        
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }

    /// <summary>
    /// Memory optimization settings for controlling resource usage
    /// </summary>
    public class MemoryOptimizationSettings
    {
        /// <summary>
        /// Maximum memory usage before triggering cleanup (MB)
        /// </summary>
        public long MaxMemoryUsageMB { get; set; } = 512;
        
        /// <summary>
        /// Force garbage collection after this many operations
        /// </summary>
        public int GCInterval { get; set; } = 50;
        
        /// <summary>
        /// Enable streaming for large datasets
        /// </summary>
        public bool EnableStreaming { get; set; } = true;
        
        /// <summary>
        /// Batch size for processing large datasets
        /// </summary>
        public int BatchSize { get; set; } = 1000;
        
        /// <summary>
        /// Enable data compression for temporary storage
        /// </summary>
        public bool EnableCompression { get; set; } = true;
        
        /// <summary>
        /// Maximum number of cached objects
        /// </summary>
        public int MaxCacheSize { get; set; } = 100;
        
        /// <summary>
        /// Enable object pooling for frequently used objects
        /// </summary>
        public bool EnableObjectPooling { get; set; } = true;
        
        /// <summary>
        /// Dispose of Excel objects immediately after use
        /// </summary>
        public bool AggressiveDisposal { get; set; } = true;
    }

    /// <summary>
    /// Caching strategy for database results
    /// </summary>
    public enum CachingStrategy
    {
        None,
        Memory,
        Disk,
        Hybrid
    }

    /// <summary>
    /// Performance optimization settings
    /// </summary>
    public class OptimizationSettings
    {
        /// <summary>
        /// Enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;
        
        /// <summary>
        /// Caching strategy for database results
        /// </summary>
        public CachingStrategy CachingStrategy { get; set; } = CachingStrategy.Memory;
        
        /// <summary>
        /// Cache timeout in minutes
        /// </summary>
        public int CacheTimeoutMinutes { get; set; } = 15;
        
        /// <summary>
        /// Enable connection pooling
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;
        
        /// <summary>
        /// Maximum database connections
        /// </summary>
        public int MaxDatabaseConnections { get; set; } = 10;
        
        /// <summary>
        /// Enable asynchronous I/O operations
        /// </summary>
        public bool EnableAsyncIO { get; set; } = true;
        
        /// <summary>
        /// Prefetch data for next combinations
        /// </summary>
        public bool EnablePrefetching { get; set; } = true;
        
        /// <summary>
        /// Number of combinations to prefetch
        /// </summary>
        public int PrefetchCount { get; set; } = 3;
        
        /// <summary>
        /// Memory optimization settings
        /// </summary>
        public MemoryOptimizationSettings MemorySettings { get; set; } = new();
    }
}
