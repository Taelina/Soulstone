using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    public static class ImageHelper
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly ConcurrentDictionary<string, bool> DownloadingUrls = new();
        private static readonly ConcurrentDictionary<string, byte> FailedUrls = new();

        public static string GetImagesDirectory()
        {
            var baseDir = string.IsNullOrEmpty(Plugin.dataLocation) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoulstoneData") : Plugin.dataLocation;
            var imgDir = Path.Combine(baseDir, "images");
            if (!Directory.Exists(imgDir))
            {
                Directory.CreateDirectory(imgDir);
            }
            return imgDir;
        }

        public static string GetCacheDirectory()
        {
            var cacheDir = Path.Combine(GetImagesDirectory(), "cache");
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            return cacheDir;
        }

        public static string CopyImageToLocalFolder(string sourceFilePath, string subFolder = "general")
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return sourceFilePath;
            }

            try
            {
                var targetDir = Path.Combine(GetImagesDirectory(), subFolder);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // If already inside targetDir, don't copy again
                var fullSource = Path.GetFullPath(sourceFilePath);
                var fullTargetDir = Path.GetFullPath(targetDir);
                if (fullSource.StartsWith(fullTargetDir, StringComparison.OrdinalIgnoreCase))
                {
                    return fullSource;
                }

                var ext = Path.GetExtension(sourceFilePath);
                var cleanFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                var safeFileName = $"{cleanFileName}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
                var destPath = Path.Combine(targetDir, safeFileName);

                File.Copy(sourceFilePath, destPath, true);
                return destPath;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to copy image from {sourceFilePath}");
                return sourceFilePath;
            }
        }

        public static string? ResolveImagePath(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return null;

            pathOrUrl = pathOrUrl.Trim();

            // Web URL
            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var cachedPath = GetCacheFilePath(pathOrUrl);
                if (File.Exists(cachedPath))
                {
                    return cachedPath;
                }

                // Trigger background download
                DownloadUrlAsync(pathOrUrl, cachedPath);
                return null;
            }

            // Local path
            if (File.Exists(pathOrUrl))
            {
                return pathOrUrl;
            }

            // Check relative to dataLocation / images
            var relativeToImages = Path.Combine(GetImagesDirectory(), pathOrUrl);
            if (File.Exists(relativeToImages))
            {
                return relativeToImages;
            }

            return null;
        }

        public static IDalamudTextureWrap? GetTexture(string? pathOrUrl)
        {
            if (Plugin.TextureProvider == null) return null;

            var resolved = ResolveImagePath(pathOrUrl);
            if (resolved != null && File.Exists(resolved))
            {
                try
                {
                    return Plugin.TextureProvider.GetFromFile(resolved).GetWrapOrDefault();
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static string GetCacheFilePath(string url)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];
            var ext = ".png";
            try
            {
                var uri = new Uri(url);
                var uriExt = Path.GetExtension(uri.AbsolutePath);
                if (!string.IsNullOrEmpty(uriExt) && uriExt.Length <= 5)
                {
                    ext = uriExt;
                }
            }
            catch { }

            return Path.Combine(GetCacheDirectory(), $"{hash}{ext}");
        }

        private static void DownloadUrlAsync(string url, string destPath)
        {
            if (FailedUrls.ContainsKey(url)) return;
            if (!DownloadingUrls.TryAdd(url, true)) return;

            Task.Run(async () =>
            {
                try
                {
                    var data = await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                    await File.WriteAllBytesAsync(destPath, data).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    FailedUrls.TryAdd(url, 0);
                    Plugin.Log?.Debug(ex, $"Failed to download image from {url}");
                }
                finally
                {
                    DownloadingUrls.TryRemove(url, out _);
                }
            });
        }

        public static void DrawThumbnailOrPlaceholder(string? pathOrUrl, Vector2 size, string placeholderText = "?", Vector4? borderColor = null, float rounding = 4.0f)
        {
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            var borderCol = borderColor ?? new Vector4(0.3f, 0.45f, 0.65f, 0.6f);
            var u32Border = ImGui.ColorConvertFloat4ToU32(borderCol);

            var texture = GetTexture(pathOrUrl);
            if (texture != null)
            {
                // Draw texture maintaining aspect ratio inside the box
                var texSize = new Vector2(texture.Width, texture.Height);
                var scale = Math.Min(size.X / texSize.X, size.Y / texSize.Y);
                var displaySize = texSize * scale;
                var offset = (size - displaySize) * 0.5f;

                // Background box
                drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.12f, 0.16f, 0.8f)), rounding);

                // Image
                ImGui.SetCursorScreenPos(pos + offset);
                ImGui.Image(texture.Handle, displaySize);

                // Border
                drawList.AddRect(pos, pos + size, u32Border, rounding, ImDrawFlags.None, 1.5f);
                ImGui.SetCursorScreenPos(pos + new Vector2(size.X, 0));
            }
            else
            {
                // Placeholder card
                drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.17f, 0.22f, 0.9f)), rounding);
                drawList.AddRect(pos, pos + size, u32Border, rounding, ImDrawFlags.None, 1.5f);

                var textSize = ImGui.CalcTextSize(placeholderText);
                var textPos = pos + (size - textSize) * 0.5f;
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudGrey2), placeholderText);

                ImGui.Dummy(size);
            }
        }
    }
}
