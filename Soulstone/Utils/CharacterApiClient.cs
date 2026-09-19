using Soulstone.Datamodels;
using Soulstone.Sync;
using System;
using System.Net.Http;
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

        public static async Task<bool> UploadCharacterSheetAsync(string serverUrl, CharacterSheet sheet, string? world = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serverUrl) || sheet == null || string.IsNullOrWhiteSpace(sheet.CharacterFullName))
                return false;

            try
            {
                var baseUri = RelayCrypto.NormalizeServerUrl(serverUrl);
                var charNameEscaped = Uri.EscapeDataString(sheet.CharacterFullName.Trim());
                var worldEscaped = !string.IsNullOrWhiteSpace(world) ? Uri.EscapeDataString(world.Trim()) : string.Empty;

                var endpoint = !string.IsNullOrEmpty(worldEscaped)
                    ? $"{baseUri}/api/characters/{charNameEscaped}/{worldEscaped}"
                    : $"{baseUri}/api/characters/{charNameEscaped}";

                var json = JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = false });
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PutAsync(endpoint, content, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to upload character sheet for '{sheet?.CharacterFullName}' to '{serverUrl}'");
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
                    var response = await HttpClient.GetAsync(endpointWithWorld, ct).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                        var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (sheet != null)
                        {
                            sheet.hiddenFields ??= new();
                            sheet.SyncResourcesWithLegacyFields();
                            return sheet;
                        }
                    }
                }

                // Fallback without world
                var endpointNoWorld = $"{baseUri}/api/characters/{charNameEscaped}";
                var fallbackResponse = await HttpClient.GetAsync(endpointNoWorld, ct).ConfigureAwait(false);
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    var json = await fallbackResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (sheet != null)
                    {
                        sheet.hiddenFields ??= new();
                        sheet.SyncResourcesWithLegacyFields();
                        return sheet;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to fetch character sheet for '{characterName}' from '{serverUrl}'");
                return null;
            }
        }

        public static async Task<bool> DeleteCharacterSheetAsync(string serverUrl, string characterName, string? world = null, CancellationToken ct = default)
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

                var response = await HttpClient.DeleteAsync(endpoint, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to delete character sheet for '{characterName}' on '{serverUrl}'");
                return false;
            }
        }
    }
}
