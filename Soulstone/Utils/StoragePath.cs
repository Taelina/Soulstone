using System;
using System.IO;

namespace Soulstone.Utils;

internal static class StoragePath
{
    public static string ForJson(string directory, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.Contains('/') || name.Contains('\\') || name is "." or ".." || name.EndsWith('.'))
            throw new ArgumentException("A valid storage name is required.", nameof(name));
        var root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(directory, name.Replace(" ", "_").ToLowerInvariant() + ".json"));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The storage path must remain inside its directory.", nameof(name));
        return path;
    }
}
