namespace BorisBar;

internal static class ClipLocator
{
    public static string? FindBuiltIn(string baseName)
    {
        var wanted = ClipCatalog.NormalizeName(baseName);

        foreach (var dir in AppPaths.BuiltInCandidateDirs())
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var ext in ClipCatalog.AudioExtensions)
            {
                var exact = Path.Combine(dir, baseName + ext);
                if (File.Exists(exact))
                {
                    return exact;
                }
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (!ClipCatalog.IsAudioFile(file))
                {
                    continue;
                }

                var stem = Path.GetFileNameWithoutExtension(file);
                if (ClipCatalog.NormalizeName(stem) == wanted)
                {
                    return file;
                }
            }
        }

        return null;
    }

    public static IReadOnlyList<string> LoadCustomClips()
    {
        AppPaths.EnsureDataDirs();
        if (!Directory.Exists(AppPaths.Custom))
        {
            return [];
        }

        return Directory.EnumerateFiles(AppPaths.Custom)
            .Where(ClipCatalog.IsAudioFile)
            .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
