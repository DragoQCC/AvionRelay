
using SQLite;

namespace AvionRelay.External;

public class SqliteOptions
{
    public string DatabasePath { get; set; } = "avionrelay.db";
    public string ConnectionString { get; set; } = string.Empty;
    public int BusyTimeout { get; set; } = 5000; // 5 seconds
    public SQLiteOpenFlags OpenFlags { get; set; } = 
        SQLiteOpenFlags.ReadWrite | 
        SQLiteOpenFlags.Create | 
        SQLiteOpenFlags.SharedCache|
        SQLiteOpenFlags.FullMutex;
}