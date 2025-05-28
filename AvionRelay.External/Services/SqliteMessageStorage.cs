using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External;

public class SqliteMessageStorage : IMessageStorage
{
    private readonly SqliteDatabaseService _sqliteDatabase;
    private readonly ILogger<SqliteMessageStorage> _logger;
    
    public SqliteMessageStorage(SqliteDatabaseService sqliteDatabase, ILogger<SqliteMessageStorage> logger)
    {
        _sqliteDatabase = sqliteDatabase;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public void StorePackage(Package package, bool inQueue)
    {
    }

    /// <inheritdoc />
    public Package? RetrieveNextPackage() => null;

    /// <inheritdoc />
    public Package? RetrievePackage(Guid messageId) => null;
    
    //TODO: This class will need the logic implemented to call the DB
    public List<Package> GetPreviousMessages(int count = 100) => [];
}