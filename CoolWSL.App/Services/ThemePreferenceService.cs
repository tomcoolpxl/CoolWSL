using System.Text.Json;

namespace CoolWSL.App.Services;

public enum AppThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2,
}

public interface IThemePreferenceService
{
    event EventHandler? ThemeChanged;

    AppThemePreference CurrentTheme { get; }

    void SetTheme(AppThemePreference theme);
}

public sealed class ThemePreferenceService : IThemePreferenceService
{
    private const string SettingsFileName = "ui-preferences.json";
    private const string ThemePropertyName = "theme";

    private readonly string settingsFilePath;
    private AppThemePreference currentTheme;

    public ThemePreferenceService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        settingsFilePath = Path.Combine(localAppData, "CoolWSL", SettingsFileName);
        currentTheme = LoadThemePreference();
    }

    public event EventHandler? ThemeChanged;

    public AppThemePreference CurrentTheme => currentTheme;

    public void SetTheme(AppThemePreference theme)
    {
        if (currentTheme == theme)
        {
            return;
        }

        currentTheme = theme;
        SaveThemePreference(theme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private AppThemePreference LoadThemePreference()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                return AppThemePreference.System;
            }

            using var stream = File.OpenRead(settingsFilePath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty(ThemePropertyName, out var themeProperty))
            {
                return AppThemePreference.System;
            }

            var themeText = themeProperty.GetString();
            return Enum.TryParse<AppThemePreference>(themeText, ignoreCase: true, out var parsedTheme)
                ? parsedTheme
                : AppThemePreference.System;
        }
        catch (Exception)
        {
            return AppThemePreference.System;
        }
    }

    private void SaveThemePreference(AppThemePreference theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

            var payload = JsonSerializer.Serialize(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [ThemePropertyName] = theme.ToString(),
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            File.WriteAllText(settingsFilePath, payload);
        }
        catch (Exception)
        {
        }
    }
}