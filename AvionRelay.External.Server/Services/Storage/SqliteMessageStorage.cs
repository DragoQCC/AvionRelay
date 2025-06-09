using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Microsoft.Extensions.Logging;
using SQLite;

namespace AvionRelay.External.Server.Services;



public class SqliteMessageStorage : IMessageStorage
{
    
    private readonly ILogger<SqliteMessageStorage> _logger;
    private readonly SqliteDatabaseService _sqliteDatabase;

    public SqliteMessageStorage(ILogger<SqliteMessageStorage> logger, SqliteDatabaseService sqliteDatabase) 
    {
        _logger = logger;
        _sqliteDatabase = sqliteDatabase;
    }
    
    /// <inheritdoc />
    public void StorePackage(Package package, bool inQueue)
    {
    }

    /// <inheritdoc />
    public Package? RetrieveNextPackage() => null;

    /// <inheritdoc />
    public Package? RetrievePackage(Guid messageId) => null;
    
    
    public async Task<bool> InsertMessageAsync(MessageRecord message)
    {
        return await _sqliteDatabase.InsertAsync(message);
    }
    
    public async Task<List<MessageRecord>> GetRecentMessagesAsync(int count = 100)
    {
        //find the message with the latest timestamp
        var latestMessage = (await _sqliteDatabase.GetItemsWhereAsync<MessageRecord>(x => true)).OrderByDescending(x => x.Timestamp).FirstOrDefault();
        if (latestMessage is null)
        {
            return [];
        }
        //get all messages prior to the latest message up to the count
        return (await _sqliteDatabase.GetItemsWhereAsync<MessageRecord>(x => x.Timestamp < latestMessage.Timestamp)).OrderByDescending(x => x.Timestamp).Take(count).ToList();
    }
    
    public async Task<Dictionary<string, int>> GetMessageTypeStatsAsync()
    {
        var results = await  _sqliteDatabase.DoWithConnectionAsync<SQLiteAsyncConnection, Task<List<MessageTypeCount>>>(async connection =>
        {
            return await connection.QueryAsync<MessageTypeCount>("SELECT MessageType, COUNT(*) AS Count FROM MessageRecord GROUP BY MessageType ORDER BY Count DESC");
        });
        return (await results).ToDictionary(x => x.MessageType, x => x.Count);
    }
    
    private class MessageTypeCount
    {
        public string MessageType { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

// Data models