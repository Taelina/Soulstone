using Microsoft.Data.Sqlite;

namespace Soulstone.SyncServer;

internal static class PublicationQueries
{
    internal static SqliteCommand Command(SqliteConnection db, SqliteTransaction? transaction,
        string sql, params (string Name, object Value)[] parameters)
    {
        var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }

    // Table identifiers come only from the two registries, never HTTP input.
    internal static int Cleanup(SqliteConnection db, SqliteTransaction transaction, string table, long oldestTicks)
    {
        using var command = Command(db, transaction,
            $"DELETE FROM {table} WHERE UpdatedTicks < $oldest", ("$oldest", oldestTicks));
        return command.ExecuteNonQuery();
    }

    internal static bool HasCapacity(SqliteConnection db, SqliteTransaction transaction, string table, int? previousBytes, int bytes)
    {
        using var command = Command(db, transaction, $"SELECT COUNT(*), COALESCE(SUM(PayloadBytes), 0) FROM {table}");
        using var reader = command.ExecuteReader();
        reader.Read();
        return (previousBytes != null || reader.GetInt64(0) < PublicationSecurity.MaximumEntries) &&
               reader.GetInt64(1) - (previousBytes ?? 0) + bytes <= PublicationSecurity.MaximumStorageBytes;
    }
}
