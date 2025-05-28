using System.Diagnostics;
using System.Linq.Expressions;
using HelpfulTypesAndExtensions;
using Microsoft.Extensions.Logging;
using SQLite;

namespace AvionRelay.External;

public class SqliteDatabaseService : IDatabaseService
{
    private readonly SqliteOptions _options;
    private SQLiteAsyncConnection? _connection;
    private readonly ILogger<SqliteDatabaseService> _logger;
    private string? _fullDatabasePath;
    
    public SqliteDatabaseService(SqliteOptions options, ILogger<SqliteDatabaseService> logger)
    {
        _options = options;
        _logger = logger;
        CreateConnection();
    }
    
# region IDatabaseService

    /// <inheritdoc />
    public async Task<bool> CreateTableAsync<T>() where T : new()
    {
        try
        {
            await _connection.CreateTableAsync<T>();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating table for type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InsertAsync<T>(T item) where T : new()
    {
        try
        {
            await _connection.InsertAsync(item);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error inserting item of type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckTableExistsAsync<T>() where T : new()
    {
        try
        {
            await _connection.CreateTableAsync<T>();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error checking if table exists for type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InsertAsync(object item, Type itemType)
    {
        try
        {
            await _connection.InsertAsync(item);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error inserting item of type {Type}", itemType.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InsertItemsAsync<T>(IEnumerable<T> items) where T : new()
    {
        try
        {
            await _connection.InsertAllAsync(items);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error inserting items of type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync<T>(T updatedItem) where T : new()
    {
        try
        {
            await _connection.UpdateAsync(updatedItem);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating item of type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateItemsAsync<T>(List<T> updatedItems) where T : new()
    {
        try
        {
            await _connection.UpdateAllAsync(updatedItems);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating items of type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync<T>(T item) where T : new()
    {
        try
        {
            await _connection.DeleteAsync(item);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting item of type {Type}", typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync<T>(string id) where T : new()
    {
        try
        {
            await _connection.DeleteAsync<T>(id);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting item of type {Type} with id {Id}", typeof(T).Name, id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(object itemToDelete)
    {
        try
        {
            await _connection.DeleteAsync(itemToDelete);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting item of type {Type}", itemToDelete.GetType().Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetItemWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        try
        {
            return await _connection.FindAsync<T>(predicate);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting item of type {Type}", typeof(T).Name);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetItemImplementingTypeWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        T? item = null;
        try
        {
            await EnsureConnection();
            
            List<TableMapping> foundTablesOfType = [];
            //find the table mapping for the type T, or any type that inherits from T
            foreach (var tableMap in _connection.TableMappings)
            {
                //if the tableMap is for a type that implements T, use that tableMap
                if (tableMap.MappedType.IsSubclassOf(typeof(T)))
                {
                    foundTablesOfType.Add(tableMap);
                }
            }
            foreach (var tableMap in foundTablesOfType)
            {
                var tableItems = await _connection.QueryAsync(tableMap, $"SELECT * FROM {tableMap.TableName} WHERE {predicate}");
                if (tableItems.Count > 0)
                {
                    item = tableItems.First() as T;
                    break;
                }
            }
            return item;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting item of type {Type}", typeof(T).Name);
            //returns an empty collection
            return item;
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> GetItemsWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        try
        {
            await EnsureConnection();
            return await _connection.Table<T>().Where(predicate).ToListAsync();
        }
        catch (SQLiteException sqlerror)
        {
            //if sql error is for no such table, create the table and try again
            if (sqlerror.Message.Contains("no such table"))
            {
                await CreateTableAsync<T>();
                return await _connection.Table<T>().Where(predicate).ToListAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting items of type {Type}", typeof(T).Name);
            //returns an empty collection
            return [];
        }
        return [];
    }

    /// <inheritdoc />
    public async Task<List<T>> GetItemsImplementingTypeWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            await EnsureConnection();
            List<T> items = [];
            List<TableMapping> foundTablesOfType = [];
            //find the table mapping for the type T, or any type that inherits from T
            //Get all of the subclasses for T
            List<TableMapping> allTableMappings = _connection.TableMappings.ToList();
           
            foreach (var tableMap in allTableMappings)
            {
                //if the tableMap is for a type that implements T, use that tableMap
                if (tableMap.MappedType.IsSubclassOf(typeof(T)))
                {
                    foundTablesOfType.Add(tableMap);
                }
            }
            foreach (var tableMap in foundTablesOfType)
            {
                try
                {
                    var tableItems = await _connection.QueryAsync(tableMap, $"SELECT * FROM {tableMap.TableName} WHERE {predicate}");
                    items.AddRange(tableItems.Cast<T>());
                }
                catch(SQLiteException sqlerror)
                {
                    //if sql error is for no such table, create the table and continue
                    if (sqlerror.Message.Contains("no such table"))
                    {
                        await _connection.CreateTableAsync(tableMap.MappedType);
                    }
                }
            }
            return items;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting items of type {Type}", typeof(T).Name);
            //returns an empty collection
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetItemByIdAsync<T>(string id) where T : class, new()
    {
        try
        {
            return await _connection.FindAsync<T>(id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting item of type {Type} with id {Id}", typeof(T).Name, id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> GetItemsOfTypeAsync<T>() where T : class, new()
    {
        try
        {
            return await _connection.Table<T>().ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting items of type {Type}", typeof(T).Name);
            return new List<T>();
        }
    }

    /// <inheritdoc />
    public async Task<TResult?> DoWithConnectionAsync<TConnection, TResult>(Func<TConnection, TResult> action)
    {
        try
        {
            return action((TConnection)(object)_connection);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error performing action with connection");
            return default;
        }
    }
  
#endregion

#region Helpers

    private async Task EnsureConnection()
    {
        bool connected = true;
        if (await IsConnectionHealthyAsync() is false)
        {
            CreateConnection();
            connected = await IsConnectionHealthyAsync();
        }
        if (connected)
        {
            return;
        }
        _logger.LogError("Failed to connect to the database");
        throw new Exception("Failed to connect to the database");
    }
    
    public string GetDatabasePath() => _fullDatabasePath ?? _options.DatabasePath;
    
    private async Task<bool> IsConnectionHealthyAsync()
    {
        if (_connection == null)
            return false;
            
        try
        {
            // Quick health check - execute a simple query
            var stopwatch = Stopwatch.StartNew();
            var result = await _connection.ExecuteScalarAsync<int>("SELECT 1");
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("SQLite health check slow: {ElapsedMs}ms",  stopwatch.ElapsedMilliseconds);
            }
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite health check failed");
            return false;
        }
    }
    
    private void EnsureDirectoryExists(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created database directory: {Directory}", directory);
        }
    }
    
    private string BuildConnectionString()
    {
        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            return _options.ConnectionString;
        }
        
        var connectionString = $"Data Source={_fullDatabasePath}";
        
        // Add additional parameters
        if (_options.BusyTimeout > 0)
        {
            connectionString += $";Busy Timeout={_options.BusyTimeout}";
        }
        
        return connectionString;
    }
    
    
    private void CreateConnection()
    {
        try
        {
            _fullDatabasePath = GetFullDatabasePath();
            EnsureDirectoryExists(_fullDatabasePath);
            var connectionString = BuildConnectionString();
            _connection = new SQLiteAsyncConnection(connectionString, _options.OpenFlags);
            _connection.EnableLoadExtensionAsync(true);
            _connection.EnableWriteAheadLoggingAsync();
            _logger.LogInformation("Created SQLite connection to {Path}", _fullDatabasePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating connection");
        }
    }
    
    private string GetFullDatabasePath()
    {
        if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            // Extract path from connection string
            var match = System.Text.RegularExpressions.Regex.Match(
                _options.ConnectionString, 
                @"Data Source=([^;]+)");
            if (match.Success)
            {
                return Path.GetFullPath(match.Groups[1].Value);
            }
        }
        
        // Use DatabasePath
        if (Path.IsPathRooted(_options.DatabasePath))
        {
            return _options.DatabasePath;
        }
        
        // Relative path - put in app data directory
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvionRelay");
            
        return Path.Combine(appDataPath, _options.DatabasePath);
    }
#endregion
}

