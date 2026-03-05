using System;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using STP_group_1.ViewModels;

namespace STP_group_1.Views.Dialogs;

public sealed class NewCanvasDialogViewModel : ViewModelBase
{
    public NewCanvasDialogViewModel(double width, double height, Color background)
    {
        _width = width;
        _height = height;
        _background = background;

        OkCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(true));
        CancelCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(false));
    }

    private double _width;
    public double Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    private double _height;
    public double Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    private Color _background;
    public Color Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }

    public ReactiveCommand<Unit, Unit> OkCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action<bool>? CloseRequested;
}
