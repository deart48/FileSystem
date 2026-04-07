using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemApp.Models;

namespace FileSystemApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private FolderItem _rootFolder;

    [ObservableProperty]
    private FolderItem _currentFolder;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _currentItems = new();

    [ObservableProperty]
    private string _newItemName = "New Item";

    [ObservableProperty]
    private long _newFileSize = 1024;

    [ObservableProperty]
    private ObservableCollection<FolderItem> _availableMoveTargets = new();

    [ObservableProperty]
    private FolderItem? _selectedMoveTarget;

    [ObservableProperty]
    private FileSystemItem? _itemPendingMove;

    [ObservableProperty]
    private bool _isMoveSelectionVisible;

    [ObservableProperty]
    private FileSystemItem? _itemPendingRename;

    [ObservableProperty]
    private string _renameItemName = string.Empty;

    [ObservableProperty]
    private bool _isRenameSelectionVisible;

    public MainWindowViewModel()
    {
        _rootFolder = new FolderItem("Root", null);
        _currentFolder = _rootFolder;
        
        AddFileCommand = new RelayCommand(AddFile);
        AddFolderCommand = new RelayCommand(AddFolder);
        CopyItemCommand = new RelayCommand<FileSystemItem>(CopyItem);
        MoveItemCommand = new RelayCommand<FileSystemItem>(StartMoveItemSelection);
        ConfirmMoveCommand = new RelayCommand(ConfirmMoveItem, CanConfirmMoveItem);
        CancelMoveCommand = new RelayCommand(CancelMoveItemSelection);
        StartRenameCommand = new RelayCommand<FileSystemItem>(StartRenameSelection);
        ConfirmRenameCommand = new RelayCommand(ConfirmRenameItem, CanConfirmRenameItem);
        CancelRenameCommand = new RelayCommand(CancelRenameSelection);
        OpenFolderCommand = new RelayCommand<FileSystemItem>(OpenFolder);
        GoBackCommand = new RelayCommand(GoBack, CanGoBack);
        
        RefreshItems();
    }

    public ICommand AddFileCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand CopyItemCommand { get; }
    public ICommand MoveItemCommand { get; }
    public RelayCommand ConfirmMoveCommand { get; }
    public ICommand CancelMoveCommand { get; }
    public ICommand StartRenameCommand { get; }
    public RelayCommand ConfirmRenameCommand { get; }
    public ICommand CancelRenameCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public RelayCommand GoBackCommand { get; }

    private void AddFile()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;
        new FileItem(NewItemName, CurrentFolder, NewFileSize);
        SetStatus($"Added file: {NewItemName} to {CurrentFolder.Name}");
        RefreshItems();
    }

    private void AddFolder()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;
        new FolderItem(NewItemName, CurrentFolder);
        SetStatus($"Added folder: {NewItemName} to {CurrentFolder.Name}");
        RefreshItems();
    }

    private void OpenFolder(FileSystemItem? item)
    {
        if (item is FolderItem folder)
        {
            CurrentFolder = folder;
            SetStatus($"Opened folder: {folder.Name}");
            RefreshItems();
        }
    }

    private void GoBack()
    {
        if (CurrentFolder.Parent != null)
        {
            CurrentFolder = CurrentFolder.Parent;
            SetStatus($"Back to: {CurrentFolder.Name}");
            RefreshItems();
        }
    }

    private bool CanGoBack() => CurrentFolder?.Parent != null;

    private void CopyItem(FileSystemItem? item)
    {
        if (item == null) return;
        FileSystemItem.Copy(item, CurrentFolder);
        SetStatus($"Copied: {item.Name}");
        RefreshItems();
    }

    partial void OnSelectedMoveTargetChanged(FolderItem? value)
    {
        ConfirmMoveCommand.NotifyCanExecuteChanged();
    }

    partial void OnRenameItemNameChanged(string value)
    {
        ConfirmRenameCommand.NotifyCanExecuteChanged();
    }

    private void StartMoveItemSelection(FileSystemItem? item)
    {
        if (item == null) return;

        var allFolders = GetAllFolders(_rootFolder)
            .Where(folder => CanMoveToFolder(item, folder))
            .ToList();

        if (allFolders.Count == 0)
        {
            SetStatus("No available target folders.");
            return;
        }

        ItemPendingMove = item;
        AvailableMoveTargets = new ObservableCollection<FolderItem>(allFolders);
        SelectedMoveTarget = AvailableMoveTargets.FirstOrDefault();
        IsMoveSelectionVisible = true;
        SetStatus($"Choose target folder for: {item.Name}");
    }

    private void ConfirmMoveItem()
    {
        if (ItemPendingMove == null || SelectedMoveTarget == null)
        {
            return;
        }

        FileSystemItem.Move(ItemPendingMove, SelectedMoveTarget);
        SetStatus($"Moved {ItemPendingMove.Name} to {SelectedMoveTarget.Name}");
        CancelMoveItemSelection();
        RefreshItems();
    }

    private bool CanConfirmMoveItem() => ItemPendingMove != null && SelectedMoveTarget != null;

    private void CancelMoveItemSelection()
    {
        ItemPendingMove = null;
        SelectedMoveTarget = null;
        AvailableMoveTargets = new ObservableCollection<FolderItem>();
        IsMoveSelectionVisible = false;
        ConfirmMoveCommand.NotifyCanExecuteChanged();
    }

    private void StartRenameSelection(FileSystemItem? item)
    {
        if (item == null) return;

        ItemPendingRename = item;
        RenameItemName = item.Name;
        IsRenameSelectionVisible = true;
        SetStatus($"Rename item: {item.Name}");
        ConfirmRenameCommand.NotifyCanExecuteChanged();
    }

    private void ConfirmRenameItem()
    {
        if (ItemPendingRename == null || string.IsNullOrWhiteSpace(RenameItemName))
        {
            return;
        }

        var oldName = ItemPendingRename.Name;
        ItemPendingRename.Name = RenameItemName.Trim();
        SetStatus($"Renamed {oldName} to {ItemPendingRename.Name}");
        CancelRenameSelection();
        RefreshItems();
    }

    private bool CanConfirmRenameItem() =>
        ItemPendingRename != null && !string.IsNullOrWhiteSpace(RenameItemName);

    private void CancelRenameSelection()
    {
        ItemPendingRename = null;
        RenameItemName = string.Empty;
        IsRenameSelectionVisible = false;
        ConfirmRenameCommand.NotifyCanExecuteChanged();
    }

    private static bool CanMoveToFolder(FileSystemItem item, FolderItem target)
    {
        if (ReferenceEquals(item, target))
        {
            return false;
        }

        if (item is FolderItem folder && IsDescendantOrSame(target, folder))
        {
            return false;
        }

        return true;
    }

    private static bool IsDescendantOrSame(FolderItem candidate, FolderItem folder)
    {
        FolderItem? current = candidate;
        while (current != null)
        {
            if (ReferenceEquals(current, folder))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private static IEnumerable<FolderItem> GetAllFolders(FolderItem root)
    {
        yield return root;
        foreach (var child in root.Items.OfType<FolderItem>())
        {
            foreach (var subfolder in GetAllFolders(child))
            {
                yield return subfolder;
            }
        }
    }

    private void RefreshItems()
    {
        CurrentItems = new ObservableCollection<FileSystemItem>(CurrentFolder.Items);
        GoBackCommand.NotifyCanExecuteChanged();
    }
}
