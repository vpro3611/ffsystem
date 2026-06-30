using FileSystemP.Core;
using FileSystemP.Core.Services;

namespace FileSystemP.Tests;

public sealed class KeyBindingSettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public KeyBindingSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FileSystemP_KeyBindingServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "ffsystem_settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void EnsureSettingsFileIsValid_CreatesDefaultSettingsFile()
    {
        var service = new KeyBindingSettingsService(_settingsPath);

        service.EnsureSettingsFileIsValid();

        Assert.True(File.Exists(_settingsPath));
        var bindings = service.GetBindings();
        Assert.Equal("Ctrl+Z", bindings["undo"]);
        Assert.Equal("F12", bindings["terminal"]);
    }

    [Fact]
    public void SetBinding_NormalizesGestureBeforeSaving()
    {
        var service = new KeyBindingSettingsService(_settingsPath);
        service.EnsureSettingsFileIsValid();

        service.SetBinding("undo", "control+y");

        Assert.Equal("Ctrl+Y", service.GetBindings()["undo"]);
    }

    [Fact]
    public void SetBinding_ConflictingGestureWithoutOverwrite_ThrowsAppException()
    {
        var service = new KeyBindingSettingsService(_settingsPath);
        service.EnsureSettingsFileIsValid();

        var exception = Assert.Throws<AppException>(() => service.SetBinding("search", "Ctrl+Z"));

        Assert.Contains("already assigned to action `undo`", exception.Message);
    }

    [Fact]
    public void EnsureSettingsFileIsValid_ReplacesInvalidBindingWithDefault()
    {
        File.WriteAllText(_settingsPath, "{\"bindings\":{\"undo\":\"Ctrl+???\"}}");
        var service = new KeyBindingSettingsService(_settingsPath);

        service.EnsureSettingsFileIsValid();

        Assert.Equal("Ctrl+Z", service.GetBindings()["undo"]);
    }

    [Fact]
    public void ResetToDefaults_RestoresOriginalBindings()
    {
        var service = new KeyBindingSettingsService(_settingsPath);
        service.EnsureSettingsFileIsValid();
        service.SetBinding("undo", "Ctrl+Y");

        service.ResetToDefaults();

        var bindings = service.GetBindings();
        Assert.Equal("Ctrl+Z", bindings["undo"]);
        Assert.Equal("Ctrl+F", bindings["search"]);
    }
}
