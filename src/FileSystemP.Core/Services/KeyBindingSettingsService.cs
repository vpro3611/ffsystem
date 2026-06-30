using System.Text.Json;
using FileSystemP.Core;

namespace FileSystemP.Core.Services;

public sealed class KeyBindingSettings
{
    public Dictionary<string, string?> Bindings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IKeyBindingSettingsService
{
    string SettingsFilePath { get; }
    IReadOnlyDictionary<string, string?> GetBindings();
    IReadOnlyCollection<string> GetSupportedActions();
    bool IsBindableAction(string action);
    string NormalizeGesture(string gesture);
    void EnsureSettingsFileIsValid();
    void ResetToDefaults();
    void SetBinding(string action, string gesture, bool overwrite = false);
    bool TryGetActionForGesture(string gesture, out string action);
}

public sealed class KeyBindingSettingsService : IKeyBindingSettingsService
{
    private const string ClassName = nameof(KeyBindingSettingsService);

    private static readonly (string Action, string? DefaultGesture)[] SupportedActionsInOrder =
    [
        ("undo", "Ctrl+Z"),
        ("back", "Alt+Left"),
        ("forward", "Alt+Right"),
        ("home", "Alt+Home"),
        ("search", "Ctrl+F"),
        ("hidden", "Ctrl+H"),
        ("terminal", "F12"),
        ("open", "Enter"),
        ("rename", "F2"),
        ("delete", "Delete"),
        ("copy", "Ctrl+C"),
        ("paste", "Ctrl+V"),
        ("newfile", "Ctrl+Alt+N"),
        ("newfilewithcontent", "Ctrl+Shift+Alt+N"),
        ("newfolder", "Ctrl+Shift+N"),
        ("properties", "Alt+Enter")
    ];

    private static readonly Dictionary<string, string> ActionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["undo"] = "undo",
        ["back"] = "back",
        ["forward"] = "forward",
        ["home"] = "home",
        ["search"] = "search",
        ["hidden"] = "hidden",
        ["togglehidden"] = "hidden",
        ["toggle-hidden"] = "hidden",
        ["terminal"] = "terminal",
        ["toggleterminal"] = "terminal",
        ["toggle-terminal"] = "terminal",
        ["open"] = "open",
        ["rename"] = "rename",
        ["delete"] = "delete",
        ["copy"] = "copy",
        ["paste"] = "paste",
        ["newfile"] = "newfile",
        ["new-file"] = "newfile",
        ["newfilewithcontent"] = "newfilewithcontent",
        ["new-file-with-content"] = "newfilewithcontent",
        ["newfolder"] = "newfolder",
        ["new-folder"] = "newfolder",
        ["properties"] = "properties",
        ["prop"] = "properties"
    };

    private static readonly Dictionary<string, string> ModifierAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = "Ctrl",
        ["control"] = "Ctrl",
        ["alt"] = "Alt",
        ["shift"] = "Shift",
        ["win"] = "Win",
        ["windows"] = "Win",
        ["meta"] = "Win"
    };

    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enter"] = "Enter",
        ["return"] = "Enter",
        ["esc"] = "Escape",
        ["escape"] = "Escape",
        ["del"] = "Delete",
        ["delete"] = "Delete",
        ["ins"] = "Insert",
        ["insert"] = "Insert",
        ["tab"] = "Tab",
        ["space"] = "Space",
        ["spacebar"] = "Space",
        ["backspace"] = "Backspace",
        ["left"] = "Left",
        ["right"] = "Right",
        ["up"] = "Up",
        ["down"] = "Down",
        ["home"] = "Home",
        ["end"] = "End",
        ["pageup"] = "PageUp",
        ["pgup"] = "PageUp",
        ["pagedown"] = "PageDown",
        ["pgdn"] = "PageDown"
    };

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    private KeyBindingSettings? _cachedSettings;

    public KeyBindingSettingsService(string? settingsFilePath = null)
    {
        SettingsFilePath = settingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileSystemP",
            "ffsystem_settings.json");
    }

    public string SettingsFilePath { get; }

    public IReadOnlyDictionary<string, string?> GetBindings()
    {
        var settings = GetOrLoadSettings();
        return new Dictionary<string, string?>(settings.Bindings, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetSupportedActions()
    {
        return SupportedActionsInOrder.Select(a => a.Action).ToArray();
    }

    public bool IsBindableAction(string action)
    {
        return TryNormalizeAction(action, out _);
    }

    public string NormalizeGesture(string gesture)
    {
        return NormalizeGestureCore(gesture);
    }

    public void EnsureSettingsFileIsValid()
    {
        var validated = ValidateSettings(LoadSettingsFromDisk());
        SaveSettings(validated);
        _cachedSettings = validated;
    }

    public void ResetToDefaults()
    {
        var defaults = CreateDefaultSettings();
        SaveSettings(defaults);
        _cachedSettings = defaults;
    }

    public void SetBinding(string action, string gesture, bool overwrite = false)
    {
        if (!TryNormalizeAction(action, out string normalizedAction))
        {
            throw new AppException($"Unknown bindable action: {action}", $"{ClassName}.{nameof(SetBinding)}()");
        }

        string normalizedGesture = NormalizeGestureCore(gesture);
        var settings = CloneSettings(GetOrLoadSettings());

        var conflict = settings.Bindings
            .FirstOrDefault(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Value) &&
                !string.Equals(kvp.Key, normalizedAction, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(kvp.Value, normalizedGesture, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(conflict.Key))
        {
            if (!overwrite)
            {
                throw new AppException(
                    $"Binding `{normalizedGesture}` is already assigned to action `{conflict.Key}`. Use `-ob` to overwrite it.",
                    $"{ClassName}.{nameof(SetBinding)}()");
            }

            settings.Bindings[conflict.Key] = null;
        }

        settings.Bindings[normalizedAction] = normalizedGesture;
        SaveSettings(settings);
        _cachedSettings = settings;
    }

    public bool TryGetActionForGesture(string gesture, out string action)
    {
        try
        {
            string normalizedGesture = NormalizeGestureCore(gesture);
            var settings = GetOrLoadSettings();
            var match = settings.Bindings.FirstOrDefault(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Value) &&
                string.Equals(kvp.Value, normalizedGesture, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match.Key))
            {
                action = match.Key;
                return true;
            }
        }
        catch
        {
            // ignore invalid runtime gesture strings and treat them as unbound
        }

        action = string.Empty;
        return false;
    }

    private KeyBindingSettings GetOrLoadSettings()
    {
        if (_cachedSettings is not null)
        {
            return _cachedSettings;
        }

        EnsureSettingsFileIsValid();
        return _cachedSettings!;
    }

    private KeyBindingSettings LoadSettingsFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(SettingsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefaultSettings();
            }

            return JsonSerializer.Deserialize<KeyBindingSettings>(json, _serializerOptions) ?? CreateDefaultSettings();
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    private KeyBindingSettings ValidateSettings(KeyBindingSettings settings)
    {
        var defaults = SupportedActionsInOrder.ToDictionary(item => item.Action, item => item.DefaultGesture, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var usedGestures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (action, defaultGesture) in SupportedActionsInOrder)
        {
            string? candidate = null;
            if (settings.Bindings.TryGetValue(action, out string? configured))
            {
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    try
                    {
                        candidate = NormalizeGestureCore(configured);
                    }
                    catch
                    {
                        candidate = defaultGesture;
                    }
                }
            }
            else
            {
                candidate = defaultGesture;
            }

            if (!string.IsNullOrWhiteSpace(candidate) && !usedGestures.Add(candidate))
            {
                string? fallback = defaultGesture;
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    fallback = NormalizeGestureCore(fallback);
                }

                candidate = !string.IsNullOrWhiteSpace(fallback) && usedGestures.Add(fallback)
                    ? fallback
                    : null;
            }

            normalized[action] = candidate;
        }

        return new KeyBindingSettings { Bindings = normalized };
    }

    private void SaveSettings(KeyBindingSettings settings)
    {
        string? directory = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private static KeyBindingSettings CreateDefaultSettings()
    {
        return new KeyBindingSettings
        {
            Bindings = SupportedActionsInOrder.ToDictionary(item => item.Action, item => item.DefaultGesture, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static KeyBindingSettings CloneSettings(KeyBindingSettings settings)
    {
        return new KeyBindingSettings
        {
            Bindings = new Dictionary<string, string?>(settings.Bindings, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool TryNormalizeAction(string action, out string normalizedAction)
    {
        normalizedAction = string.Empty;
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        if (ActionAliases.TryGetValue(action.Trim(), out string? alias))
        {
            normalizedAction = alias;
            return true;
        }

        return false;
    }

    private static string NormalizeGestureCore(string gesture)
    {
        if (string.IsNullOrWhiteSpace(gesture))
        {
            throw new AppException("Binding cannot be empty.", $"{ClassName}.{nameof(NormalizeGestureCore)}()");
        }

        string[] parts = gesture
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            throw new AppException("Binding cannot be empty.", $"{ClassName}.{nameof(NormalizeGestureCore)}()");
        }

        var modifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;

        foreach (string part in parts)
        {
            if (ModifierAliases.TryGetValue(part, out string? normalizedModifier))
            {
                if (!modifiers.Add(normalizedModifier!))
                {
                    throw new AppException($"Duplicate modifier `{part}` in binding `{gesture}`.", $"{ClassName}.{nameof(NormalizeGestureCore)}()");
                }

                continue;
            }

            if (key is not null)
            {
                throw new AppException($"Binding `{gesture}` must contain exactly one non-modifier key.", $"{ClassName}.{nameof(NormalizeGestureCore)}()");
            }

            key = NormalizeKey(part, gesture);
        }

        if (key is null)
        {
            throw new AppException($"Binding `{gesture}` must include a key.", $"{ClassName}.{nameof(NormalizeGestureCore)}()");
        }

        var ordered = new List<string>();
        if (modifiers.Contains("Ctrl")) ordered.Add("Ctrl");
        if (modifiers.Contains("Alt")) ordered.Add("Alt");
        if (modifiers.Contains("Shift")) ordered.Add("Shift");
        if (modifiers.Contains("Win")) ordered.Add("Win");
        ordered.Add(key);
        return string.Join('+', ordered);
    }

    private static string NormalizeKey(string value, string originalGesture)
    {
        if (KeyAliases.TryGetValue(value, out string? normalizedKey))
        {
            return normalizedKey!;
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 1 && char.IsLetterOrDigit(trimmed[0]))
        {
            return char.ToUpperInvariant(trimmed[0]).ToString();
        }

        if (trimmed.Length > 1 && (trimmed[0] == 'F' || trimmed[0] == 'f') &&
            int.TryParse(trimmed[1..], out int functionIndex) && functionIndex is >= 1 and <= 24)
        {
            return $"F{functionIndex}";
        }

        throw new AppException($"Unsupported key `{value}` in binding `{originalGesture}`.", $"{ClassName}.{nameof(NormalizeKey)}()");
    }
}
