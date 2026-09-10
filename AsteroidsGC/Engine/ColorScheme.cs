using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace AsteroidsGC.Engine;

public sealed class ColorScheme
{
    public static ColorScheme Current { get; private set; } = BuildDefault();

    public static void Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ColorSchemeData>(json, options);
            if (data is null)
            {
                return;
            }

            Current = new ColorScheme
            {
                Ship = ParseColor(data.Ship),
                Asteroid = ParseColor(data.Asteroid),
                Bullet = ParseColor(data.Bullet),
                Saucer = ParseColor(data.Saucer),
                Particle = ParseColor(data.Particle),
                Text = ParseColor(data.Text),
                ThrustFlame = ParseColor(data.ThrustFlame),
                TitleLogoPrimary = ParseColor(data.TitleLogoPrimary),
                TitleLogoSecondary = ParseColor(data.TitleLogoSecondary),
                Name = data.Name,
                SourcePath = path,
            };
        }
        catch (Exception ex)
            when (ex is IOException
                or JsonException
                or NotSupportedException
                or FormatException)
        {
            // Keep the previously active scheme if the file is missing or malformed.
        }
    }

    public static IReadOnlyList<ColorSchemePreset> DiscoverPresets(
        string directory,
        int maxCount = 9)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var files = Directory.GetFiles(directory, "*.json")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount);

        var presets = new List<ColorSchemePreset>();
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                if (doc.RootElement.TryGetProperty("name", out var nameProp)
                    && nameProp.GetString() is { } jsonName)
                {
                    name = jsonName;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                // Fall back to the file name if the preset can't be read.
            }

            presets.Add(new ColorSchemePreset(file, name));
        }

        return presets;
    }

    private static Color ParseColor(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);

    private static ColorScheme BuildDefault()
    {
        var asteroid = Color.FromRgb(210, 255, 220);
        return new ColorScheme
        {
            Ship = Colors.White,
            Asteroid = asteroid,
            Bullet = Colors.White,
            Saucer = Color.FromRgb(160, 255, 170),
            Particle = Color.FromRgb(255, 225, 160),
            Text = Colors.White,
            ThrustFlame = Colors.OrangeRed,
            TitleLogoPrimary = asteroid,
            TitleLogoSecondary = asteroid,
            Name = "Classic",
            SourcePath = null,
        };
    }

    private sealed class ColorSchemeData
    {
        public string Name { get; set; } = "";
        public string Ship { get; set; } = "#FFFFFF";
        public string Asteroid { get; set; } = "#D2FFDC";
        public string Bullet { get; set; } = "#FFFFFF";
        public string Saucer { get; set; } = "#A0FFAA";
        public string Particle { get; set; } = "#FFE1A0";
        public string Text { get; set; } = "#FFFFFF";
        public string ThrustFlame { get; set; } = "#FF4500";
        public string TitleLogoPrimary { get; set; } = "#D2FFDC";
        public string TitleLogoSecondary { get; set; } = "#D2FFDC";
    }

    public required Color Ship { get; init; }
    public required Color Asteroid { get; init; }
    public required Color Bullet { get; init; }
    public required Color Saucer { get; init; }
    public required Color Particle { get; init; }
    public required Color Text { get; init; }
    public required Color ThrustFlame { get; init; }
    public required Color TitleLogoPrimary { get; init; }
    public required Color TitleLogoSecondary { get; init; }
    public string Name { get; init; } = "Classic";
    public string? SourcePath { get; init; }
}
