using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Services;
using Metalama.Extensions.DependencyInjection;

namespace AvionRelay.Core;


public class AvionRelayOptions
{
    public string ApplicationName { get; set; } = "AvionRelay";
    public bool EnableMessagePersistence { get; set; } = true;
    public RetryPolicy RetryPolicy { get; set; } = new DefaultRetryPolicy();
    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public StorageOptions StorageConfig { get; set; } = new();
   
    
    public AvionRelayOptions WithDefaultTimeout(TimeSpan timeout)
    {
        MessageTimeout = timeout;
        return this;
    }
    
    public AvionRelayOptions WithRetryPolicy(RetryPolicy retryPolicy)
    {
        RetryPolicy = retryPolicy;
        return this;
    }
}

public class StorageOptions
{
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory;
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// How long to keep messages before cleanup
    /// </summary>
    public int MessageRetentionDays { get; set; } = 7;
    
    /// <summary>
    /// Enable compression for stored messages
    /// </summary>
    public bool EnableCompression { get; set; } = true;
    
    /// <summary>
    /// Maximum message size to store (in bytes)
    /// </summary>
    public long MaxMessageSize { get; set; } = 10 * 1024 * 1024; // 10MB
    
    /// <summary>
    /// Path for file-based storage (SQLite, etc.)
    /// </summary>
    public string? DatabasePath { get; set; }
}

