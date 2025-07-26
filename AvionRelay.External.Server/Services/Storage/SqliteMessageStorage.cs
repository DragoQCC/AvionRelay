using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;



public class SqliteMessageStorage : IExternalMessageStorage
{
    
    private readonly ILogger<SqliteMessageStorage> _logger;
    private readonly SqliteDatabaseService _sqliteDatabase;

    public SqliteMessageStorage(ILogger<SqliteMessageStorage> logger, SqliteDatabaseService sqliteDatabase) 
    {
        _logger = logger;
        _sqliteDatabase = sqliteDatabase;
    }


    /// <inheritdoc />
    public async Task StoreTransportPackage(TransportPackage transportPackage)
    {
    }

    /// <inheritdoc />
    public async Task StoreMessageContext(MessageContext messageContext)
    {
    }

    /// <inheritdoc />
    public async Task StoreMessageForSchedule()
    {
    }

    /// <inheritdoc />
    public async Task StoreMessageForFailure()
    {
    }
}