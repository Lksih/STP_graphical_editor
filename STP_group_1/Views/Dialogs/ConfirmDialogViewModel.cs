using System;
using System.Reactive;
using ReactiveUI;
using STP_group_1.ViewModels;

namespace STP_group_1.Views.Dialogs;

public sealed class ConfirmDialogViewModel : ViewModelBase
{
    public ConfirmDialogViewModel(string message)
    {
        Message = message;

        OkCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(true));
        CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(false));
    }

    public string Message { get; }

    public ReactiveCommand<Unit, Unit> OkCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action<bool>? CloseRequested;
}
