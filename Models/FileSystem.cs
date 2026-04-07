using System;
using System.Collections.Generic;
using System.Linq;

namespace FileSystemApp.Models;

public enum FileSystemType
{
    Folder,
    File
}

public abstract class FileSystemItem
{
    public string Name { get; set; }
    public FolderItem? Parent { get; set; }

    public string Location
    {
        get
        {
            if (Parent == null) return Name;
            return $"{Parent.Location}/{Name}";
        }
    }

    public abstract FileSystemType Type { get; }
    public abstract long Size { get; }

    protected FileSystemItem(string name, FolderItem? parent)
    {
        Name = name;
        Parent = parent;
    }

    public static FileSystemItem Copy(FileSystemItem item, FolderItem destination)
    {
        if (item is FileItem file)
        {
            return new FileItem(file.Name + "_copy", destination, file.Size);
        }
        else if (item is FolderItem folder)
        {
            var newFolder = new FolderItem(folder.Name + "_copy", destination);
            foreach (var child in folder.Items)
            {
                Copy(child, newFolder);
            }
            return newFolder;
        }
        throw new ArgumentException("Unknown item type");
    }


    public static void Move(FileSystemItem item, FolderItem destination)
    {
        if (item.Parent != null)
        {
            item.Parent.RemoveItem(item);
        }
        item.Parent = destination;
        destination.AddItem(item);
    }
}

public class FileItem : FileSystemItem
{
    private readonly long _size;

    public override FileSystemType Type => FileSystemType.File;
    public override long Size => _size;

    public FileItem(string name, FolderItem? parent, long size) : base(name, parent)
    {
        _size = size;
        parent?.AddItem(this);
    }
}

public class FolderItem : FileSystemItem
{
    private readonly List<FileSystemItem> _items = new();

    public override FileSystemType Type => FileSystemType.Folder;
    public override long Size => _items.Sum(i => i.Size);

    public IReadOnlyList<FileSystemItem> Items => _items;

    public FolderItem(string name, FolderItem? parent) : base(name, parent)
    {
        parent?.AddItem(this);
    }

    public void AddItem(FileSystemItem item)
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    public void RemoveItem(FileSystemItem item)
    {
        _items.Remove(item);
    }
}
