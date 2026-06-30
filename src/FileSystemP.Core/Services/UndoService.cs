using System;
using System.Collections.Generic;
using System.IO;

namespace FileSystemP.Core.Services;

public interface IUndoAction
{
    void Execute();
}

public interface IUndoService
{
    void Push(IUndoAction action);
    void Undo();
    bool CanUndo { get; }
    event EventHandler CanUndoChanged;
}

public class UndoService : IUndoService
{
    private readonly Stack<IUndoAction> _undoStack = new();
    public bool CanUndo => _undoStack.Count > 0;
    public event EventHandler? CanUndoChanged;

    public void Push(IUndoAction action)
    {
        _undoStack.Push(action);
        CanUndoChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var action = _undoStack.Pop();
            action.Execute();
            CanUndoChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

public record UndoRenameAction(string CurrentPath, string OriginalPath) : IUndoAction
{
    public void Execute()
    {
        string originalName = Path.GetFileName(OriginalPath);
        FileDirectorySystemService.Rename(CurrentPath, originalName);
    }
}

public record UndoCreateAction(string Path) : IUndoAction
{
    public void Execute()
    {
        FileDirectorySystemService.Delete(Path);
    }
}

public record UndoMoveAction(string CurrentPath, string OriginalPath) : IUndoAction
{
    public void Execute()
    {
        FileDirectorySystemService.Move(CurrentPath, OriginalPath);
    }
}

public record UndoDeleteAction(string OriginalPath) : IUndoAction
{
    public void Execute()
    {
        // Use Shell32 to restore from Recycle Bin
        // ssfBITBUCKET = 10
        Type? shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType == null)
        {
            Console.WriteLine("Could not find Shell.Application type.");
            return;
        }

        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null)
        {
            Console.WriteLine("Could not create Shell.Application instance.");
            return;
        }

        dynamic recycleBin = shell.NameSpace(10);
        bool found = false;

        // Normalize search path
        string normalizedOriginal = Path.GetFullPath(OriginalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (dynamic item in recycleBin.Items())
        {
            string itemName = recycleBin.GetDetailsOf(item, 0);
            string itemOriginalLocation = recycleBin.GetDetailsOf(item, 1);

            string fullItemPath = Path.Combine(itemOriginalLocation, itemName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(fullItemPath, normalizedOriginal, StringComparison.OrdinalIgnoreCase))
            {
                bool verbInvoked = false;
                foreach (dynamic verb in item.Verbs())
                {
                    string verbName = verb.Name.Replace("&", "");
                    // Try English and common localizations if needed,
                    // but "Restore" is often the internal name even if displayed differently.
                    // However, we can also check for specific properties if this fails.
                    if (string.Equals(verbName, "Restore", StringComparison.OrdinalIgnoreCase))
                    {
                        verb.DoIt();
                        verbInvoked = true;
                        break;
                    }
                }

                if (!verbInvoked)
                {
                    item.InvokeVerb("restore");
                }

                found = true;
                break;
            }
        }

        if (!found)
        {
            Console.WriteLine($"Could not find '{OriginalPath}' in Recycle Bin.");
        }
    }
}
