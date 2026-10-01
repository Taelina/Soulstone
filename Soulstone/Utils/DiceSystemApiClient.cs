using Soulstone.Datamodels;
using Soulstone.Sync;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    internal sealed record PublishedDiceSystem(
        string Code,
        string PlayerName,
        string WorldName,
        string SystemName,
        string Payload,
        DateTimeOffset UpdatedAtUtc);

    internal sealed record DiceSystemVersion(string Code, string SystemName, DateTimeOffset UpdatedAtUtc);

    internal sealed class DiceSystemApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient httpClient;

        public static DiceSystemApiClient Shared { get; } = new(new HttpClient { Timeout = TimeSpan.FromSeconds(10) });

        internal DiceSystemApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<PublishedDiceSystem?> PublishAsync(
            string serverUrl,
            DiceSystem system,
            string playerName,
            string worldName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || system == null ||
                string.IsNullOrWhiteSpace(system.systemName) || string.IsNullOrWhiteSpace(playerName))
                return null;

            try
            {
                var payload = JsonSerializer.Serialize(system, JsonOptions);
                var request = new
                {
                    PlayerName = playerName.Trim(),
                    WorldName = worldName?.Trim() ?? string.Empty,
                    SystemName = system.systemName.Trim(),
                    Payload = payload,
                    Code = string.IsNullOrWhiteSpace(system.publishedCode) ? null : system.publishedCode
                };
                using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"{RelayCrypto.NormalizeServerUrl(serverUrl)}/api/dice-systems", content, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return JsonSerializer.Deserialize<PublishedDiceSystem>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to publish dice system to '{serverUrl}'");
                return null;
            }
        }

        public async Task<PublishedDiceSystem?> DownloadAsync(string serverUrl, string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || !IsValidCode(code))
                return null;

            return await GetAsync<PublishedDiceSystem>(serverUrl, code.Trim(), string.Empty, ct).ConfigureAwait(false);
        }

        public async Task<DiceSystemVersion?> GetVersionAsync(string serverUrl, string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || !IsValidCode(code))
                return null;

            return await GetAsync<DiceSystemVersion>(serverUrl, code.Trim(), "/version", ct).ConfigureAwait(false);
        }

        public static DiceSystem? DeserializeSystem(PublishedDiceSystem published)
        {
            try
            {
                var system = JsonSerializer.Deserialize<DiceSystem>(published.Payload, JsonOptions);
                if (system == null)
                    return null;

                system.publishedCode = published.Code;
                system.publishedAtUtc = published.UpdatedAtUtc;
                system.publisherPlayerName = published.PlayerName;
                system.publisherWorldName = published.WorldName;
                return system;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<T?> GetAsync<T>(string serverUrl, string code, string suffix, CancellationToken ct)
        {
            try
            {
                var endpoint = $"{RelayCrypto.NormalizeServerUrl(serverUrl)}/api/dice-systems/{Uri.EscapeDataString(code)}{suffix}";
                var response = await httpClient.GetAsync(endpoint, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return default;

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to fetch dice system '{code}' from '{serverUrl}'");
                return default;
            }
        }

        private static bool IsValidCode(string code) =>
            code.Trim().Length == 10 && code.Trim().All(char.IsLetterOrDigit);
    }
}
