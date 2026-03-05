using System.Collections.ObjectModel;
using Avalonia.Media;
using Geometry;
using ReactiveUI;

namespace STP_group_1.ViewModels;

public sealed class LayerViewModel : ViewModelBase
{
    private string _name = "Layer";
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private IBrush _previewBrush = Brushes.Transparent;
    public IBrush PreviewBrush
    {
        get => _previewBrush;
        set => this.RaiseAndSetIfChanged(ref _previewBrush, value);
    }

    /// <summary>
    /// Геометрические фигуры, принадлежащие этому слою.
    /// </summary>
    public ObservableCollection<IFigure> Figures { get; } = new();
}
