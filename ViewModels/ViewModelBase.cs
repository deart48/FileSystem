using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileSystemApp.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    protected void SetStatus(string message) => StatusMessage = message;

    protected void ResetStatus() => SetStatus("Ready");

    protected void SetBusy(bool isBusy) => IsBusy = isBusy;

    protected void RunSafely(Action action, string? errorContext = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            SetStatus(errorContext is null ? ex.Message : $"{errorContext}: {ex.Message}");
        }
    }
}
