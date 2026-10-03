using Microsoft.Data.Sqlite;

namespace Soulstone.SyncServer;

public sealed class PublicationStorageOptions
{
    public string DatabasePath { get; set; } = string.Empty;
    public string KeyFile { get; set; } = string.Empty;
}

/// <summary>Owns the encrypted publication store. No plaintext or volatile fallback.</summary>
public sealed class PublicationDatabase : IDisposable
{
    private static readonly Lazy<bool> ProviderInitialized = new(() =>
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        SQLitePCL.raw.FreezeProvider();
        return true;
    });

    private readonly object syncRoot = new();
    private readonly SqliteConnection connection;

    public PublicationDatabase(PublicationStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DatabasePath) || !Path.IsPathFullyQualified(options.DatabasePath))
            throw new InvalidOperationException("Storage:DatabasePath must be an absolute file path outside the deployment directory.");
        string key = ReadSecret(options.KeyFile, "Storage:KeyFile");
        if (key.Length != 64 || !key.All(char.IsAsciiHexDigit))
            throw new InvalidOperationException("Storage:KeyFile must contain a random 32-byte key encoded as 64 hexadecimal characters.");

        try
        {
            _ = ProviderInitialized.Value;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            throw new InvalidOperationException("A compatible SQLCipher native library is required. Use the Community edition Docker image or supply binaries for this platform.");
        }

        // Validate configuration before creating anything. Never generate a replacement key.
        Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            DefaultTimeout = 5,
        }.ToString());

        try
        {
            connection.Open();
            // Apply the key after Open so failed decryption still has a disposable native handle.
            // The value was restricted to hexadecimal above; no SQL metacharacters are possible.
            Execute(connection, "PRAGMA key = '" + key + "'");
            using var cipher = connection.CreateCommand();
            cipher.CommandText = "PRAGMA cipher_version";
            if (cipher.ExecuteScalar() is not string version || !version.StartsWith("4.", StringComparison.Ordinal))
                throw new InvalidOperationException("SQLCipher 4 database encryption is required; ordinary SQLite is not supported.");

            // Force a page read with the supplied key before making any schema changes.
            Execute(connection, "SELECT count(*) FROM sqlite_master");
            Execute(connection, "PRAGMA temp_store = MEMORY; PRAGMA secure_delete = ON; PRAGMA journal_mode = DELETE; PRAGMA synchronous = FULL;");
            InitializeSchema();
        }
        catch (Exception ex) when (ex is SqliteException or InvalidOperationException)
        {
            connection.Dispose();
            // Native diagnostics may contain SQL. Keep configuration and content out of errors.
            throw new InvalidOperationException("Encrypted publication storage could not be opened. Check the key, database integrity, and file permissions.");
        }
    }

    internal T Read<T>(Func<SqliteConnection, T> operation)
    {
        lock (syncRoot)
        {
            try { return operation(connection); }
            catch (SqliteException)
            {
                throw new InvalidOperationException("Encrypted publication storage read failed.");
            }
        }
    }

    internal T Write<T>(Func<SqliteConnection, SqliteTransaction, T> operation)
    {
        lock (syncRoot)
        {
            // An immediate transaction also serializes writers from other connections/processes.
            try
            {
                using var transaction = connection.BeginTransaction(deferred: false);
                T result = operation(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch (SqliteException)
            {
                throw new InvalidOperationException("Encrypted publication storage write failed.");
            }
        }
    }

    public bool IsHealthy()
    {
        try
        {
            return Read(db =>
            {
                using var command = db.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master";
                return command.ExecuteScalar() is long;
            });
        }
        catch (InvalidOperationException) { return false; }
    }

    private void InitializeSchema()
    {
        Write((db, transaction) =>
        {
            using var command = db.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA user_version";
            long version = (long)command.ExecuteScalar()!;
            if (version > 1)
                throw new InvalidOperationException("The publication database schema is newer than this server.");
            if (version == 0)
            {
                command.CommandText = """
                    CREATE TABLE CharacterProfiles (
                        NameKey TEXT NOT NULL,
                        WorldKey TEXT NOT NULL,
                        Payload TEXT NOT NULL,
                        OwnerHash BLOB NOT NULL,
                        PayloadBytes INTEGER NOT NULL,
                        UpdatedTicks INTEGER NOT NULL,
                        PRIMARY KEY (NameKey, WorldKey)
                    );
                    CREATE INDEX CharacterProfilesUpdated ON CharacterProfiles (UpdatedTicks);
                    CREATE TABLE DiceSystems (
                        Code TEXT NOT NULL PRIMARY KEY,
                        PlayerName TEXT NOT NULL,
                        WorldName TEXT NOT NULL,
                        SystemName TEXT NOT NULL,
                        Payload TEXT NOT NULL,
                        OwnerHash BLOB NOT NULL,
                        PayloadBytes INTEGER NOT NULL,
                        UpdatedTicks INTEGER NOT NULL
                    );
                    CREATE INDEX DiceSystemsUpdated ON DiceSystems (UpdatedTicks);
                    PRAGMA user_version = 1;
                    """;
                command.ExecuteNonQuery();
            }
            return true;
        });
    }

    private static string ReadSecret(string path, string setting)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new InvalidOperationException(setting + " must point to an absolute, access-restricted secret file.");
        try
        {
            string value = File.ReadAllText(path).Trim();
            if (value.Length is 0 or > 8192)
                throw new InvalidOperationException(setting + " is empty or too large.");
            return value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(setting + " could not be read. Check that the service identity can access it.");
        }
    }

    private static void Execute(SqliteConnection db, string sql)
    {
        using var command = db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        lock (syncRoot)
            connection.Dispose();
    }
}
