using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Soulstone.SyncServer;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Soulstone.SyncServer.Tests;

public class PublicationPersistenceTests
{
    private static readonly string Owner = new('A', 64);
    private static readonly string Other = new('B', 64);

    [Fact]
    public void Reopening_PreservesPublicationsOwnershipCodesAndVersion()
    {
        using var storage = new TestStorage();
        var clock = new MutableTimeProvider();
        PublishedDiceSystemResponse first;
        using (var database = storage.Open())
        {
            Sheets(database, clock).Store("Player", "Moogle", "{\"saved\":true}", Owner).Should().Be(RegistryWriteResult.Success);
            Systems(database, clock).TryPublish(new("Player", "Moogle", "Rules", "{}"), out var published, Owner).Should().BeTrue();
            first = published!;
        }
        clock.Now += TimeSpan.FromDays(1);
        using (var database = storage.Open())
        {
            var sheets = Sheets(database, clock);
            sheets.TryGet("player", "moogle", out var payload).Should().BeTrue();
            payload.Should().Be("{\"saved\":true}");
            sheets.Store("Player", "Moogle", "{}", Other).Should().Be(RegistryWriteResult.Unauthorized);
            sheets.Delete("Player", "Moogle", Other).Should().Be(RegistryWriteResult.Unauthorized);
            var systems = Systems(database, clock);
            systems.TryGet(first.Code.ToLowerInvariant(), out var restored).Should().BeTrue();
            restored.Should().Be(first);
            systems.TryGetVersion(first.Code, out var version).Should().BeTrue();
            version!.UpdatedAtUtc.Should().Be(first.UpdatedAtUtc);
            systems.Publish(new("Player", "Moogle", "Rules", "{}", first.Code), Other, out _)
                .Should().Be(RegistryWriteResult.Unauthorized);
            systems.TryPublish(new("Player", "Moogle", "Updated", "{\"v\":2}", first.Code), out _, Owner).Should().BeTrue();
            sheets.Delete("Player", "Moogle", Owner).Should().Be(RegistryWriteResult.Success);
        }
        using (var database = storage.Open())
        {
            Sheets(database, clock).TryGet("Player", "Moogle", out _).Should().BeFalse();
            Systems(database, clock).TryGet(first.Code, out var updated).Should().BeTrue();
            updated!.Payload.Should().Be("{\"v\":2}");
            updated.UpdatedAtUtc.Should().Be(clock.Now);
        }
    }

    [Fact]
    public void Reopening_DoesNotRefreshExpiry_AndExpiredReadsFailBeforeCleanup()
    {
        using var storage = new TestStorage();
        var clock = new MutableTimeProvider();
        string code;
        using (var database = storage.Open())
        {
            Sheets(database, clock).TryStore("Player", "Moogle", "{}", Owner).Should().BeTrue();
            Systems(database, clock).TryPublish(new("Player", "Moogle", "Rules", "{}"), out var published, Owner).Should().BeTrue();
            code = published!.Code;
        }
        clock.Now += TimeSpan.FromDays(7);
        using (var database = storage.Open())
        {
            var sheets = Sheets(database, clock);
            sheets.TryGet("Player", "Moogle", out _).Should().BeTrue();
            clock.Now = clock.Now.AddTicks(1);
            sheets.TryGet("Player", "Moogle", out _).Should().BeFalse();
            sheets.TryGet("Player", null, out _).Should().BeFalse();
            sheets.Store("Player", "Moogle", "{}", Other).Should().Be(RegistryWriteResult.Success);
            clock.Now += TimeSpan.FromDays(24);
            var systems = Systems(database, clock);
            systems.TryGet(code, out _).Should().BeFalse();
            systems.TryGetVersion(code, out _).Should().BeFalse();
            systems.CleanupExpired().Should().Be(1);
        }
    }

    [Fact]
    public void Database_IsEncrypted_AndCannotBeReadWithoutTheKey()
    {
        using var storage = new TestStorage();
        const string marker = "unique-content-encryption-check-6e3a";
        using (var database = storage.Open())
            Sheets(database, TimeProvider.System).TryStore(marker, "Moogle", "{\"text\":\"" + marker + "\"}", Owner).Should().BeTrue();

        string fileText = Encoding.UTF8.GetString(File.ReadAllBytes(storage.Options.DatabasePath));
        fileText.Should().NotStartWith("SQLite format 3").And.NotContain(marker).And.NotContain(Owner);
        using var unkeyed = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = storage.Options.DatabasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ToString());
        unkeyed.Open();
        using var command = unkeyed.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master";
        Action read = () => command.ExecuteScalar();
        read.Should().Throw<SqliteException>();
    }

    [Fact]
    public void WrongOrMissingKeys_FailClosed_WithoutReplacingTheDatabase()
    {
        using var storage = new TestStorage();
        using (var database = storage.Open())
            Sheets(database, TimeProvider.System).TryStore("Player", "Moogle", "{}", Owner).Should().BeTrue();
        string originalKey = File.ReadAllText(storage.Options.KeyFile);
        byte[] originalFile = File.ReadAllBytes(storage.Options.DatabasePath);
        string wrongKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(storage.Options.KeyFile, wrongKey);
        Action reopen = () => { using var database = storage.Open(); };
        reopen.Should().Throw<InvalidOperationException>().Which.ToString().Should().NotContain(wrongKey).And.NotContain(originalKey);
        File.ReadAllBytes(storage.Options.DatabasePath).Should().Equal(originalFile);
        File.Delete(storage.Options.KeyFile);
        reopen.Should().Throw<InvalidOperationException>();
        File.WriteAllText(storage.Options.KeyFile, originalKey);
        using var restored = storage.Open();
        Sheets(restored, TimeProvider.System).TryGet("Player", "Moogle", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentClaimsAcrossConnections_HaveOnlyOneOwner()
    {
        using var storage = new TestStorage();
        using var firstDatabase = storage.Open();
        using var secondDatabase = storage.Open();
        var first = Sheets(firstDatabase, TimeProvider.System);
        var second = Sheets(secondDatabase, TimeProvider.System);
        using var start = new ManualResetEventSlim();
        Task<RegistryWriteResult> Claim(CharacterSheetRegistry registry, string token) => Task.Run(() =>
        {
            start.Wait();
            return registry.Store("Player", "Moogle", "{}", token);
        });
        var one = Claim(first, Owner);
        var two = Claim(second, Other);
        start.Set();
        var results = await Task.WhenAll(one, two);
        results.Should().BeEquivalentTo(new[] { RegistryWriteResult.Success, RegistryWriteResult.Unauthorized });
    }

    [Fact]
    public void FailedWrite_RollsBackPayloadAndUpdateTimestamp()
    {
        using var storage = new TestStorage();
        var clock = new MutableTimeProvider();
        PublishedDiceSystemResponse first;
        using (var database = storage.Open())
        {
            var systems = Systems(database, clock);
            systems.TryPublish(new("Player", "Moogle", "Rules", "{\"v\":1}"), out var value, Owner).Should().BeTrue();
            first = value!;
            using var inspection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = storage.Options.DatabasePath, Password = File.ReadAllText(storage.Options.KeyFile), Pooling = false,
            }.ToString());
            inspection.Open();
            using var trigger = inspection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER RejectUpdate BEFORE UPDATE ON DiceSystems BEGIN SELECT RAISE(ABORT, 'test write failure'); END";
            trigger.ExecuteNonQuery();
            clock.Now += TimeSpan.FromDays(1);
            Action update = () => systems.Publish(new("Player", "Moogle", "Rules", "{\"v\":2}", first.Code), Owner, out _);
            update.Should().Throw<InvalidOperationException>();
        }
        using var restored = storage.Open();
        Systems(restored, clock).TryGet(first.Code, out var unchanged).Should().BeTrue();
        unchanged.Should().Be(first);
    }

    [Fact]
    public void MalformedKey_DoesNotCreateAnUnencryptedDatabase()
    {
        using var storage = new TestStorage();
        File.WriteAllText(storage.Options.KeyFile, "not-a-random-32-byte-key");
        Action open = () => { using var database = storage.Open(); };
        open.Should().Throw<InvalidOperationException>();
        File.Exists(storage.Options.DatabasePath).Should().BeFalse();
    }

    [Fact]
    public void PlaintextDatabase_IsRejectedWithoutModification()
    {
        using var storage = new TestStorage();
        // Opening once initializes the native provider; then replace with a plaintext fixture.
        using (var database = storage.Open()) { }
        File.Delete(storage.Options.DatabasePath);
        using (var plaintext = new SqliteConnection(new SqliteConnectionStringBuilder
               { DataSource = storage.Options.DatabasePath, Pooling = false }.ToString()))
        {
            plaintext.Open();
            using var command = plaintext.CreateCommand();
            command.CommandText = "CREATE TABLE PublicData(Value TEXT); INSERT INTO PublicData VALUES ('original')";
            command.ExecuteNonQuery();
        }
        byte[] original = File.ReadAllBytes(storage.Options.DatabasePath);
        Action open = () => { using var database = storage.Open(); };
        open.Should().Throw<InvalidOperationException>();
        File.ReadAllBytes(storage.Options.DatabasePath).Should().Equal(original);
    }

    [Fact]
    public void OwnershipCredentials_AreStoredOnlyAsHashes()
    {
        using var storage = new TestStorage();
        using (var database = storage.Open())
            Sheets(database, TimeProvider.System).TryStore("Player", "Moogle", "{}", Owner).Should().BeTrue();
        using var inspection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = storage.Options.DatabasePath, Password = File.ReadAllText(storage.Options.KeyFile), Pooling = false,
        }.ToString());
        inspection.Open();
        using var command = inspection.CreateCommand();
        command.CommandText = "SELECT OwnerHash FROM CharacterProfiles";
        ((byte[])command.ExecuteScalar()!).Should().Equal(SHA256.HashData(Encoding.UTF8.GetBytes(Owner)));
    }

    [Fact]
    public async Task HttpHostRestart_PreservesPublicDownloadsAndRejectsAnotherOwner()
    {
        using var storage = new TestStorage();
        const string path = "/api/characters/RestartPlayer/Moogle";
        RelayWebApplicationFactory CreateHost() => new(storage.Options);
        using (var host = CreateHost())
        using (var client = host.CreateClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Owner);
            using var response = await client.PutAsync(path, new StringContent("{\"saved\":true}", Encoding.UTF8, "application/json"));
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        using (var host = CreateHost())
        using (var client = host.CreateClient())
        {
            (await client.GetStringAsync(path)).Should().Be("{\"saved\":true}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Other);
            using var response = await client.DeleteAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    private static CharacterSheetRegistry Sheets(PublicationDatabase db, TimeProvider clock) => new(db, clock, NullLogger<CharacterSheetRegistry>.Instance);
    private static DiceSystemRegistry Systems(PublicationDatabase db, TimeProvider clock) => new(db, clock, NullLogger<DiceSystemRegistry>.Instance);

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
