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
