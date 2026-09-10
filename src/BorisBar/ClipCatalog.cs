using System.Text;

namespace BorisBar;

internal readonly record struct BuiltInClip(string Label, string BaseName);

internal static class ClipCatalog
{
    public static readonly BuiltInClip[] BuiltIns =
    [
        new("Fai uno sforzo", "fai uno sforzo"),
        new("Tutti basiti", "Tutti basiti"),
        new("A cazzo di cane", "a cazzo di cane"),
        new("F4", "F4"),
        new("Fiano Romano", "Fiano Romano"),
        new("Però sei molto italiano", "Però sei molto italiano"),
        new("Thank you for being so not italian", "Thank you for being so not italian"),
        new("Io la mollo questa serie", "Io la mollo questa serie"),
        new("Vuoi una pompa", "Vuoi una pompa")
    ];

    public static readonly string[] AudioExtensions =
    [
        ".mp3", ".mp4", ".m4a", ".wav", ".aiff", ".aif", ".caf", ".ogg"
    ];

    public static string NormalizeName(string name)
    {
        return name.Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public static bool IsAudioFile(string path)
    {
        var ext = Path.GetExtension(path);
        return AudioExtensions.Any(known => ext.Equals(known, StringComparison.OrdinalIgnoreCase));
    }
}
