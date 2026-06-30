# Keybinding and Settings Guide

## Overview

FileSystemP supports configurable keyboard shortcuts for common application actions.

These shortcuts are stored in a per-user JSON settings file and can be managed from the built-in terminal.

This document describes:

- where the settings file lives
- how startup validation works
- which terminal commands manage keybindings
- which actions can currently be bound
- what the default bindings are
- how conflicts are handled

## Settings File Location

The keybinding configuration is stored here:

```text
%LocalAppData%\FileSystemP\ffsystem_settings.json
```

This resolves to a per-user writable location such as:

```text
C:\Users\YourUserName\AppData\Local\FileSystemP\ffsystem_settings.json
```

### Why this location is used

This location was chosen because it is:

- user-specific
- writable without administrator rights
- safe even when the application is installed in a protected folder
- appropriate for persistent desktop-app settings

## File Format

The settings file stores an action-to-binding map.

Example:

```json
{
  "Bindings": {
    "undo": "Ctrl+Z",
    "back": "Alt+Left",
    "forward": "Alt+Right",
    "home": "Alt+Home",
    "search": "Ctrl+F",
    "hidden": "Ctrl+H",
    "terminal": "F12",
    "open": "Enter",
    "rename": "F2",
    "delete": "Delete",
    "copy": "Ctrl+C",
    "paste": "Ctrl+V",
    "newfile": "Ctrl+Alt+N",
    "newfilewithcontent": "Ctrl+Shift+Alt+N",
    "newfolder": "Ctrl+Shift+N",
    "properties": "Alt+Enter"
  }
}
```

A binding may also be cleared internally and appear as `null`, which means the action is currently unbound.

## Startup Validation

On application startup, FileSystemP validates the settings file before the main window begins using shortcuts.

Validation behavior includes:

- creating the file if it does not exist
- restoring defaults if the file is empty or malformed
- normalizing supported gestures such as `control+y` into `Ctrl+Y`
- rejecting unsupported or invalid key definitions
- repairing conflicting bindings so one shortcut is not assigned to multiple actions

This means the app tries to recover to a valid, usable keybinding state automatically.

## Terminal Commands

Keybindings can be managed through the built-in terminal.

### Set a binding

```text
set <action> <binding>
```

Example:

```text
set undo Ctrl+Z
set search Ctrl+Shift+F
```

### Overwrite an existing binding

If a binding is already assigned to another action, use:

```text
set <action> <binding> -ob
```

or:

```text
set <action> <binding> --overbind
```

Example:

```text
set search Ctrl+Z -ob
```

This reassigns `Ctrl+Z` to `search` and clears it from the previous action.

### List current bindings

```text
binds
```

This prints the current action-to-shortcut map from the settings file.

### Restore defaults

```text
resetbinds
```

This rewrites the settings file with the default shortcut set.

## Supported Bindable Actions

The following actions can currently be configured:

- `undo`
- `back`
- `forward`
- `home`
- `search`
- `hidden`
- `terminal`
- `open`
- `rename`
- `delete`
- `copy`
- `paste`
- `newfile`
- `newfilewithcontent`
- `newfolder`
- `properties`

## Default Bindings

The default keymap is:

| Action | Default binding |
| --- | --- |
| `undo` | `Ctrl+Z` |
| `back` | `Alt+Left` |
| `forward` | `Alt+Right` |
| `home` | `Alt+Home` |
| `search` | `Ctrl+F` |
| `hidden` | `Ctrl+H` |
| `terminal` | `F12` |
| `open` | `Enter` |
| `rename` | `F2` |
| `delete` | `Delete` |
| `copy` | `Ctrl+C` |
| `paste` | `Ctrl+V` |
| `newfile` | `Ctrl+Alt+N` |
| `newfilewithcontent` | `Ctrl+Shift+Alt+N` |
| `newfolder` | `Ctrl+Shift+N` |
| `properties` | `Alt+Enter` |

## Gesture Rules

Supported gestures follow a simple modifier-plus-key format.

Examples:

- `Ctrl+Z`
- `Ctrl+Shift+F`
- `Alt+Enter`
- `F12`
- `Alt+Left`

### Normalization behavior

The settings service normalizes common aliases. For example:

- `control+z` becomes `Ctrl+Z`
- `return` becomes `Enter`
- `del` becomes `Delete`
- `esc` becomes `Escape`

### Current constraints

A binding must contain:

- zero or more supported modifiers: `Ctrl`, `Alt`, `Shift`, `Win`
- exactly one non-modifier key

Unsupported or malformed gestures are rejected.

## Conflict Handling

A single shortcut cannot be assigned to multiple actions at the same time.

When a conflict happens:

- `set <action> <binding>` fails with an error
- `set <action> <binding> -ob` reassigns the shortcut and clears it from the previous action

## Runtime Behavior

Shortcut changes made through the terminal are persisted immediately and are used by the running application without requiring a restart.

Shortcuts are currently handled through the main window's preview-key pipeline, so they behave as application-wide commands rather than narrowly scoped control-level shortcuts.

## Related Documentation

- [README](../README.md)
- [User Guide](user-guide.md)
- [Developer Guide](developer-guide.md)
- [Architecture](architecture.md)
