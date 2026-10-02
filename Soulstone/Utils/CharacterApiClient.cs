using Soulstone.Datamodels;
using Soulstone.Sync;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    internal static class CharacterApiClient
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<bool> UploadCharacterSheetAsync(string serverUrl, CharacterSheet sheet, string? world = null, string? characterName = null, CancellationToken ct = default, string? ownerToken = null)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || sheet == null || string.IsNullOrWhiteSpace(ownerToken))
                return false;

            string targetCharName = !string.IsNullOrWhiteSpace(characterName)
                ? characterName
                : (!string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : string.Empty);

            if (string.IsNullOrWhiteSpace(targetCharName))
                return false;

            try
            {
                var baseUri = RelayCrypto.NormalizeServerUrl(serverUrl);
                var charNameEscaped = Uri.EscapeDataString(targetCharName.Trim());
                var worldEscaped = !string.IsNullOrWhiteSpace(world) ? Uri.EscapeDataString(world.Trim()) : string.Empty;

                var endpoint = !string.IsNullOrEmpty(worldEscaped)
                    ? $"{baseUri}/api/characters/{charNameEscaped}/{worldEscaped}"
                    : $"{baseUri}/api/characters/{charNameEscaped}";

                var json = PublicCharacterProfile.FromSheet(sheet).ToJson();
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
                using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to upload public character profile");
                return false;
            }
        }

        public static async Task<CharacterSheet?> FetchCharacterSheetAsync(string serverUrl, string characterName, string? world = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(characterName))
                return null;

            try
            {
                var baseUri = RelayCrypto.NormalizeServerUrl(serverUrl);
                var charNameEscaped = Uri.EscapeDataString(characterName.Trim());
                var worldEscaped = !string.IsNullOrWhiteSpace(world) ? Uri.EscapeDataString(world.Trim()) : string.Empty;

                // Try with world if provided
                if (!string.IsNullOrEmpty(worldEscaped))
                {
                    var endpointWithWorld = $"{baseUri}/api/characters/{charNameEscaped}/{worldEscaped}";
                    using var response = await HttpClient.GetAsync(endpointWithWorld, ct).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                        var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (sheet != null)
                        {
                            sheet.hiddenFields ??= new();
                            return sheet;
                        }
                    }
                }

                // World-qualified requests must not fall back to another world.
                if (!string.IsNullOrEmpty(worldEscaped)) return null;

                // Fallback without world
                var endpointNoWorld = $"{baseUri}/api/characters/{charNameEscaped}";
                using var fallbackResponse = await HttpClient.GetAsync(endpointNoWorld, ct).ConfigureAwait(false);
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    var json = await fallbackResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (sheet != null)
                    {
                        sheet.hiddenFields ??= new();
                        return sheet;
                    }
                }

                return null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to fetch public character profile");
                return null;
            }
        }

        public static async Task<bool> DeleteCharacterSheetAsync(string serverUrl, string characterName, string? world = null, CancellationToken ct = default, string? ownerToken = null)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(characterName))
                return false;

            try
            {
                var baseUri = RelayCrypto.NormalizeServerUrl(serverUrl);
                var charNameEscaped = Uri.EscapeDataString(characterName.Trim());
                var worldEscaped = !string.IsNullOrWhiteSpace(world) ? Uri.EscapeDataString(world.Trim()) : string.Empty;

                var endpoint = !string.IsNullOrEmpty(worldEscaped)
                    ? $"{baseUri}/api/characters/{charNameEscaped}/{worldEscaped}"
                    : $"{baseUri}/api/characters/{charNameEscaped}";

                if (string.IsNullOrWhiteSpace(ownerToken)) return false;
                using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
                using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to delete public character profile");
                return false;
            }
        }
    }
}
