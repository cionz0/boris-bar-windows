namespace BorisBar;

internal static class AppPaths
{
    public static string DataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "boris-bar");

    public static string Builtin => Path.Combine(DataRoot, "builtin");

    public static string Custom => Path.Combine(DataRoot, "custom");

    public static string DisclaimerFile
    {
        get
        {
            var nextToExe = Path.Combine(AppContext.BaseDirectory, "DISCLAIMER.txt");
            if (File.Exists(nextToExe))
            {
                return nextToExe;
            }

            var fromRepo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "DISCLAIMER.txt"));
            return File.Exists(fromRepo) ? fromRepo : nextToExe;
        }
    }

    public static string? ExeDirectory
    {
        get
        {
            var path = Environment.ProcessPath;
            return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path);
        }
    }

    public static IEnumerable<string> BuiltInCandidateDirs()
    {
        yield return Builtin;

        if (ExeDirectory is string exeDir)
        {
            yield return Path.Combine(exeDir, "assets", "clips");
            yield return Path.Combine(exeDir, "assets", "clips", "Archive");
        }

        var assets = Path.Combine(AppContext.BaseDirectory, "assets");
        yield return Path.Combine(assets, "clips");
        yield return Path.Combine(assets, "clips", "Archive");
    }

    public static void EnsureDataDirs()
    {
        Directory.CreateDirectory(Builtin);
        Directory.CreateDirectory(Custom);
    }
}
