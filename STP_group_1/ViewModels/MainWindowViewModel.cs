using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DynamicData;
using Geometry;
using ReactiveUI;
using STP_group_1.Services;
using STP_group_1.Views.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Geometry.Graphic;

namespace STP_group_1.ViewModels;

public enum ToolKind
{
    Move,
    Eraser,
    Line,
    Polygon,
    Ellipse,
    Curve,
    CurvedPolygon,
    Fill
}

public enum ActiveColorTarget
{
    Foreground,
    Background
}

public interface ICanvasInteractionHandler
{
    bool HandleCanvasPointerPressed(Geometry.Point modelPoint, bool isLeftButtonPressed, bool isRightButtonPressed, KeyModifiers modifiers, double hitTolerance, IEnumerable<IFigure> figures, out bool shouldStartDragging);
    void HandleCanvasDragDelta(double dx, double dy);
    public void HandleCanvasPointerReleased(double dx, double dy);
}

public sealed class MainWindowViewModel : ViewModelBase, ICanvasInteractionHandler
{
    private readonly IUiDialogService _dialogs;
    private readonly IEditorIoService _io;
    private readonly UndoRedoManager _undoRedoManager = new();

    public string UndoDescription => _undoRedoManager.UndoDescription;
    public string RedoDescription => _undoRedoManager.RedoDescription;

    private void OnUndoRedoManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndoRedoManager.UndoDescription))
            this.RaisePropertyChanged(nameof(UndoDescription));
        else if (e.PropertyName == nameof(UndoRedoManager.RedoDescription))
            this.RaisePropertyChanged(nameof(RedoDescription));
    }
    private bool _isColorPickerVisible;
    public bool IsColorPickerVisible
    {
        get => _isColorPickerVisible;
        set => this.RaiseAndSetIfChanged(ref _isColorPickerVisible, value);
    }

    public ObservableCollection<Geometry.Point> PressedPoints { get; } = new();

    private Geometry.Point PreviousCenter = new Geometry.Point(0, 0);

    public MainWindowViewModel(IUiDialogService dialogs, IEditorIoService io)
    {
        _dialogs = dialogs;
        _io = io;
        _undoRedoManager.PropertyChanged += OnUndoRedoManagerPropertyChanged;

        var initialLayer = new LayerViewModel { Name = "Background", PreviewBrush = Brushes.White };
        AttachLayer(initialLayer);
        Layers.Add(initialLayer);
        SelectedLayer = Layers.FirstOrDefault();
        SelectedTool = ToolKind.Move;
        SelectedTheme = EditorTheme.System;

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t switch
            {
                ToolKind.Move => "Перемещение",
                ToolKind.Eraser => "Ластик",
                ToolKind.Line => "Линия",
                ToolKind.Polygon => "Многоугольник",
                ToolKind.Ellipse => "Эллипс",
                ToolKind.Curve => "Кривая",
                ToolKind.CurvedPolygon => "Кривой полином",
                ToolKind.Fill => "Заливка",
                _ => t.ToString()
            })
            .ToProperty(this, x => x.SelectedToolDisplayName, out _selectedToolDisplayName);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Move)
            .ToProperty(this, x => x.IsMoveTool, out _isMoveTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Eraser)
            .ToProperty(this, x => x.IsEraserTool, out _isEraserTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Line)
            .ToProperty(this, x => x.IsLineTool, out _isLineTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Polygon)
            .ToProperty(this, x => x.IsPolygonTool, out _isPolygonTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Ellipse)
            .ToProperty(this, x => x.IsEllipseTool, out _isEllipseTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Curve)
            .ToProperty(this, x => x.IsCurveTool, out _isCurveTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.CurvedPolygon)
            .ToProperty(this, x => x.IsCurvedPolygonTool, out _isCurvedPolygonTool);

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t == ToolKind.Fill)
            .ToProperty(this, x => x.IsFillTool, out _isFillTool);

        this.WhenAnyValue(x => x.ZoomPercent)
            .Select(z => z / 100.0)
            .ToProperty(this, x => x.ZoomFactor, out _zoomFactor);

        this.WhenAnyValue(x => x.ZoomPercent)
            .Select(z => z > 100)
            .ToProperty(this, x => x.IsMiniMapVisible, out _isMiniMapVisible);

        this.WhenAnyValue(x => x.ZoomPercent)
            .Subscribe(z =>
            {
                var text = $"{z}%";
                if (_zoomPercentText != text)
                {
                    _zoomPercentText = text;
                    this.RaisePropertyChanged(nameof(ZoomPercentText));
                }
            });

        this.WhenAnyValue(x => x.CanvasWidth, x => x.ZoomFactor)
            .Select(t => t.Item1 * t.Item2)
            .ToProperty(this, x => x.CanvasWidthZoomed, out _canvasWidthZoomed);

        this.WhenAnyValue(x => x.CanvasHeight, x => x.ZoomFactor)
            .Select(t => t.Item1 * t.Item2)
            .ToProperty(this, x => x.CanvasHeightZoomed, out _canvasHeightZoomed);

        this.WhenAnyValue(x => x.ForegroundColor)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .Subscribe(PushRecentColor);

        this.WhenAnyValue(x => x.CanvasBackground)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .Subscribe(PushRecentColor);

        this.WhenAnyValue(x => x.SelectedTheme)
            .Skip(1)
            .Subscribe(ApplyTheme);

        RecentColors.CollectionChanged += (_, __) => this.RaisePropertyChanged(nameof(HasRecentColors));

        Layers.CollectionChanged += (_, __) => RaiseLayersChanged();

        SelectToolCommand = ReactiveCommand.Create<string?>(SelectTool);

        NewCommand = ReactiveCommand.CreateFromTask(New);
        OpenCommand = ReactiveCommand.CreateFromTask(Open);
        SaveCommand = ReactiveCommand.CreateFromTask(Save);
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        ImportCommand = ReactiveCommand.CreateFromTask(Import);

        UndoCommand = ReactiveCommand.Create(
            () => _undoRedoManager.Undo(),
            this.WhenAnyValue(x => x._undoRedoManager.CanUndo));

        RedoCommand = ReactiveCommand.Create(
            () => _undoRedoManager.Redo(),
            this.WhenAnyValue(x => x._undoRedoManager.CanRedo));

        _undoRedoManager.CommandExecuted += (s, e) =>
        {
            IsDirty = true;
            (App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.GetVisualDescendants().OfType<GeometryCanvas>().FirstOrDefault()?.Refresh();
        };

        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);

        RotateSelectedFigureCommand = ReactiveCommand.Create(RotateSelectedFigure);
        IncreaseSelectedFigureCommand = ReactiveCommand.Create(IncreaseSelectedFigure);
        DecreaseSelectedFigureCommand = ReactiveCommand.Create(DecreaseSelectedFigure);

        SelectThemeCommand = ReactiveCommand.Create<string?>(SelectTheme);

        NewLayerCommand = ReactiveCommand.Create(NewLayer);

        SelectActiveColorTargetCommand = ReactiveCommand.Create<string?>(SelectActiveColorTarget);

        var canDeleteLayer = this.WhenAnyValue(x => x.SelectedLayer)
            .Select(_ => CanDeleteLayer());

        DeleteLayerCommand = ReactiveCommand.Create(DeleteLayer, canDeleteLayer);

        ApplyRecentColorCommand = ReactiveCommand.Create<object?>(ApplyRecentColor);

        InitializeDemoFigures();
    }

    public void ExecuteCommand(IUndoRedoCommand command)
    {
        _undoRedoManager.ExecuteCommand(command);
    }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    private string? _currentProjectPath;
    public string? CurrentProjectPath
    {
        get => _currentProjectPath;
        set => this.RaiseAndSetIfChanged(ref _currentProjectPath, value);
    }

    private double _canvasWidth = 1024;
    public double CanvasWidth
    {
        get => _canvasWidth;
        set => this.RaiseAndSetIfChanged(ref _canvasWidth, value);
    }

    private double _canvasHeight = 768;
    public double CanvasHeight
    {
        get => _canvasHeight;
        set => this.RaiseAndSetIfChanged(ref _canvasHeight, value);
    }

    private Color _canvasBackground = Colors.White;
    public Color CanvasBackground
    {
        get => _canvasBackground;
        set => this.RaiseAndSetIfChanged(ref _canvasBackground, value);
    }

    private ToolKind _selectedTool;
    public ToolKind SelectedTool
    {
        get => _selectedTool;
        set => this.RaiseAndSetIfChanged(ref _selectedTool, value);
    }

    private readonly ObservableAsPropertyHelper<string> _selectedToolDisplayName;
    public string SelectedToolDisplayName => _selectedToolDisplayName.Value;

    private readonly ObservableAsPropertyHelper<bool> _isMoveTool;
    public bool IsMoveTool => _isMoveTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isEraserTool;
    public bool IsEraserTool => _isEraserTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isLineTool;
    public bool IsLineTool => _isLineTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isPolygonTool;
    public bool IsPolygonTool => _isPolygonTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isEllipseTool;
    public bool IsEllipseTool => _isEllipseTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isCurveTool;
    public bool IsCurveTool => _isCurveTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isCurvedPolygonTool;
    public bool IsCurvedPolygonTool => _isCurvedPolygonTool.Value;

    private readonly ObservableAsPropertyHelper<bool> _isFillTool;
    public bool IsFillTool => _isFillTool.Value;


    public bool IsForegroundActive => ActiveColorTarget == ActiveColorTarget.Foreground;
    public bool IsCanvasBackgroundActive => ActiveColorTarget == ActiveColorTarget.Background;

    private Color _foregroundColor = Colors.Black;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set => this.RaiseAndSetIfChanged(ref _foregroundColor, value);
    }

    private ActiveColorTarget _activeColorTarget = ActiveColorTarget.Foreground;
    public ActiveColorTarget ActiveColorTarget
    {
        get => _activeColorTarget;
        set
        {
            this.RaiseAndSetIfChanged(ref _activeColorTarget, value);
            this.RaisePropertyChanged(nameof(ActiveColor));
            this.RaisePropertyChanged(nameof(IsForegroundActive));
            this.RaisePropertyChanged(nameof(IsCanvasBackgroundActive));
        }
    }


    private void ApplyRecentColor(object? value)
    {
        if (value is Color c)
        {
            ActiveColor = c;
            return;
        }

        if (value is string s)
        {
            try
            {
                ActiveColor = Color.Parse(s);
            }
            catch
            {
                // игнорируем некорректные строки
            }
        }
    }


    public Color ActiveColor
    {
        get => ActiveColorTarget == ActiveColorTarget.Foreground ? ForegroundColor : CanvasBackground;
        set
        {
            if (ActiveColorTarget == ActiveColorTarget.Foreground)
                ForegroundColor = value;
            else
                CanvasBackground = value;

            this.RaisePropertyChanged(nameof(ActiveColor));
        }
    }

    private void SelectActiveColorTarget(string? target)
    {
        if (!Enum.TryParse<ActiveColorTarget>(target, ignoreCase: true, out var parsed))
            return;

        if (ActiveColorTarget == parsed)
        {
            IsColorPickerVisible = !IsColorPickerVisible;
            return;
        }

        ActiveColorTarget = parsed;
        IsColorPickerVisible = true;
    }

    public ObservableCollection<Color> RecentColors { get; } = new();
    public ObservableCollection<IFigure> VisibleFigures { get; } = new();
    public Dictionary<IFigure, IFigureGraphicProperties> VisibleFiguresGraphicProperties { get; } = new();

    public bool HasRecentColors => RecentColors.Count > 0;

    private int _zoomPercent = 100;
    public int ZoomPercent
    {
        get => _zoomPercent;
        set => this.RaiseAndSetIfChanged(ref _zoomPercent, Clamp(value, 10, 800));
    }

    private readonly ObservableAsPropertyHelper<double> _zoomFactor;
    public double ZoomFactor => _zoomFactor.Value;

    private readonly ObservableAsPropertyHelper<bool> _isMiniMapVisible;
    public bool IsMiniMapVisible => _isMiniMapVisible.Value;

    private readonly ObservableAsPropertyHelper<double> _canvasWidthZoomed;
    public double CanvasWidthZoomed => _canvasWidthZoomed.Value;

    private readonly ObservableAsPropertyHelper<double> _canvasHeightZoomed;
    public double CanvasHeightZoomed => _canvasHeightZoomed.Value;

    private string _zoomPercentText = "100%";
    public string ZoomPercentText
    {
        get => _zoomPercentText;
        set
        {
            if (_zoomPercentText == value)
                return;

            this.RaiseAndSetIfChanged(ref _zoomPercentText, value);

            var text = value?.Trim() ?? string.Empty;
            text = text.Replace("%", "").Trim();

            if (int.TryParse(text, out var parsed))
            {
                ZoomPercent = parsed;
            }
        }
    }

    public enum EditorTheme
    {
        System,
        Light,
        Dark
    }

    private EditorTheme _selectedTheme;
    public EditorTheme SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }

    private static void ApplyTheme(EditorTheme theme)
    {
        if (Application.Current is not Application app)
            return;

        app.RequestedThemeVariant = theme switch
        {
            EditorTheme.Light => ThemeVariant.Light,
            EditorTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    public ObservableCollection<LayerViewModel> Layers { get; } = new();

    private LayerViewModel? _selectedLayer;
    public LayerViewModel? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLayer, value);
            this.RaisePropertyChanged(nameof(CurrentLayerFigures));
        }
    }

    private void RebuildVisibleFigures()
    {
        VisibleFigures.Clear();
        VisibleFiguresGraphicProperties.Clear();

        foreach (var layer in Layers.Where(l => l.IsVisible))
        {
            foreach (var figure in layer.Figures)
            {
                VisibleFigures.Add(figure);
                if (layer.FiguresGraphicProperties.TryGetValue(figure, out var figureGraphicProperties))
                    VisibleFiguresGraphicProperties[figure] = figureGraphicProperties;
            }
        }
    }

    private void RaiseLayersChanged()
    {
        RebuildVisibleFigures();

        this.RaisePropertyChanged(nameof(CurrentLayerFigures));
        this.RaisePropertyChanged(nameof(SelectedLayer));
    }

    private void AttachLayer(LayerViewModel layer)
    {
        layer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LayerViewModel.IsVisible))
            {
                if (!layer.IsVisible && SelectedFigure is not null && layer.Figures.Contains(SelectedFigure))
                    SelectedFigure = null;

                if (!layer.IsVisible && ReferenceEquals(SelectedLayer, layer))
                    SelectedLayer = Layers.FirstOrDefault(l => l.IsVisible);

                RaiseLayersChanged();
            }
        };

        layer.Figures.CollectionChanged += (_, __) => RaiseLayersChanged();
    }

    private static readonly ObservableCollection<IFigure> EmptyFigures = new();
    private static readonly Dictionary<IFigure, IFigureGraphicProperties> EmptyFiguresGraphicProperties = new();

    // Фигуры активного слоя. Для невырбранного слоя возвращается пустая коллекция.
    public ObservableCollection<IFigure> CurrentLayerFigures => SelectedLayer?.Figures ?? EmptyFigures;
    public Dictionary<IFigure, IFigureGraphicProperties> CurrentLayerFiguresGraphicProperties => SelectedLayer?.FiguresGraphicProperties ?? EmptyFiguresGraphicProperties;

    private IFigure? _selectedFigure;
    public IFigure? SelectedFigure
    {
        get => _selectedFigure;
        set => this.RaiseAndSetIfChanged(ref _selectedFigure, value);
    }

    private Geometry.Point? _lineStart;
    private readonly List<Geometry.Point> _polygonPoints = new();
    private Geometry.Point? _ellipseCenter;
    private readonly List<Geometry.Point> _curvePoints = new();
    private readonly List<Geometry.Point> _curvedPolygonPoints = new();

    public ReactiveCommand<string?, Unit> SelectToolCommand { get; }

    public ReactiveCommand<Unit, Unit> NewCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    public ReactiveCommand<Unit, Unit> IncreaseSelectedFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> DecreaseSelectedFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> RotateSelectedFigureCommand { get; }

    public ReactiveCommand<string?, Unit> SelectThemeCommand { get; }

    public ReactiveCommand<Unit, Unit> NewLayerCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteLayerCommand { get; }

    public ReactiveCommand<string?, Unit> SelectActiveColorTargetCommand { get; }

    public ReactiveCommand<object?, Unit> ApplyRecentColorCommand { get; }

    private void SelectTool(string? tool)
    {
        if (Enum.TryParse<ToolKind>(tool, ignoreCase: true, out var parsed))
        {
            // Повторный клик по уже активному инструменту выключает его
            // и возвращает в нейтральный режим перемещения.
            var nextTool = (SelectedTool == parsed && parsed != ToolKind.Move)
                ? ToolKind.Move
                : parsed;

            if (SelectedTool == nextTool)
                return;

            // Сбрасываем наборы точек ввода при переключении режимов
            // (чтобы случайно не создать фигуру из частично введенных данных).
            _lineStart = null;
            _polygonPoints.Clear();
            _ellipseCenter = null;
            _curvePoints.Clear();
            _curvedPolygonPoints.Clear();

            SelectedTool = nextTool;
        }
    }


    private async Task New()
    {
        if (!await ConfirmLoseChangesIfDirtyAsync())
            return;

        var options = await _dialogs.ShowNewCanvasDialogAsync(CanvasWidth, CanvasHeight, CanvasBackground);
        if (options is null)
            return;

        CanvasWidth = options.Value.Width;
        CanvasHeight = options.Value.Height;
        CanvasBackground = options.Value.Background;

        Layers.Clear();

        var layer = new LayerViewModel
        {
            Name = "Layer 1",
            PreviewBrush = new SolidColorBrush(CanvasBackground)
        };

        AttachLayer(layer);
        Layers.Add(layer);
        SelectedLayer = layer;

        RaiseLayersChanged();

        CurrentProjectPath = null;
        IsDirty = false;
    }

    private async Task Open()
    {
        if (!await ConfirmLoseChangesIfDirtyAsync())
            return;

        var path = await _dialogs.PickOpenFileAsync(new[] { ".stp", ".json" });
        if (path is null)
            return;

        var (figures, figuresGraphicProperties) = await _io.OpenNativeProjectAsync(path);
        if (figures.Count == 0)
            return;

        var layer = new LayerViewModel
        {
            Name = $"Импортированный слой {Layers.Count + 1}",
            PreviewBrush = Brushes.LightBlue
        };

        AttachLayer(layer);
        Layers.Add(layer);
        SelectedLayer = layer;

        foreach (var figure in figures)
        {
            layer.FiguresGraphicProperties[figure] = figuresGraphicProperties[figure];
            layer.Figures.Add(figure);
        }

        CurrentProjectPath = path;
        IsDirty = true;
    }

    private async Task Save()
    {
        var path = CurrentProjectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = await _dialogs.PickSaveFileAsync(defaultExtension: ".stp", suggestedFileName: "project.stp");
            if (path is null)
                return;
            CurrentProjectPath = path;
        }

        var allFigures = Layers.SelectMany(layer => layer.Figures);
        var allProperties = Layers
            .SelectMany(layer => layer.FiguresGraphicProperties)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        await _io.SaveNativeProjectAsync(path, allFigures, allProperties);
        IsDirty = false;
    }

    private async Task Import()
    {
        if (!await ConfirmLoseChangesIfDirtyAsync())
            return;

        var path = await _dialogs.PickOpenFileAsync(new[] { ".svg" });
        if (path is null)
            return;

        var (figures, figuresGraphicProperties) = _io.ImportSVG(path);
        if (figures.Count == 0)
            return;

        var layer = new LayerViewModel
        {
            Name = $"Импортированный слой {Layers.Count + 1}",
            PreviewBrush = Brushes.LightBlue
        };

        AttachLayer(layer);
        Layers.Add(layer);
        SelectedLayer = layer;

        foreach (var figure in figures)
        {
            layer.FiguresGraphicProperties[figure] = figuresGraphicProperties[figure];
            layer.Figures.Add(figure);
        }

        IsDirty = true;
    }

    private async Task Export()
    {
        var path = await _dialogs.PickSaveFileAsync(defaultExtension: ".svg", suggestedFileName: "export.svg");
        if (path is null)
            return;

        var allFigures = Layers.SelectMany(layer => layer.Figures);
        var allProperties = Layers
            .SelectMany(layer => layer.FiguresGraphicProperties)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        _io.ExportSVGAndRaster(path, allFigures, allProperties, CanvasWidth, CanvasHeight);
    }

    public Task<bool> CanCloseAsync() => ConfirmLoseChangesIfDirtyAsync();

    private void ZoomIn() => ZoomPercent = Clamp(ZoomPercent + 10, 10, 800);

    private void ZoomOut() => ZoomPercent = Clamp(ZoomPercent - 10, 10, 800);


    private void DeleteFigure(IFigure figure)
    {
        var command = new DeleteFigureCommand(CurrentLayerFigures, CurrentLayerFiguresGraphicProperties, figure);
        ExecuteCommand(command);

        IsDirty = true;
    }

    private void RotateSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        // Поворот на 15 градусов (в радианах)
        const double stepDegrees = 15.0;
        var angle = Math.PI * stepDegrees / 180.0;

        var command = new RotateFigureCommand(SelectedFigure, angle);
        ExecuteCommand(command);

        IsDirty = true;
    }

    private void IncreaseSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        // Увеличение на 1.1
        var command = new RadialScaleFigureCommand(SelectedFigure, 1.1);
        ExecuteCommand(command);

        IsDirty = true;
    }

    private void DecreaseSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        // Уменьшение к 0.9
        var command = new RadialScaleFigureCommand(SelectedFigure, 0.9);
        ExecuteCommand(command);

        IsDirty = true;
    }

    private void AddNewFigure(IFigure figure)
    {
        var figureGraphicProperties = new FigureGraphicProperties(ForegroundColor, 2.0);

        var command = new AddFigureCommand(CurrentLayerFigures, CurrentLayerFiguresGraphicProperties, figure, figureGraphicProperties);
        ExecuteCommand(command);

        PressedPoints.Clear();

        SelectedFigure = figure;
        IsDirty = true;
    }

    private void SelectTheme(string? theme)
    {
        if (Enum.TryParse<EditorTheme>(theme, ignoreCase: true, out var parsed))
            SelectedTheme = parsed;
    }

    private void NewLayer()
    {
        var layer = new LayerViewModel
        {
            Name = $"Layer {Layers.Count + 1}",
            PreviewBrush = Brushes.LightGray
        };

        AttachLayer(layer);
        Layers.Add(layer);
        SelectedLayer = layer;
        IsDirty = true;
    }

    private void DeleteLayer()
    {
        if (SelectedLayer is null)
            return;

        var idx = Layers.IndexOf(SelectedLayer);
        Layers.Remove(SelectedLayer);
        SelectedLayer = Layers.Count == 0 ? null : Layers[Math.Clamp(idx, 0, Layers.Count - 1)];
        IsDirty = true;
    }

    private bool CanDeleteLayer() => SelectedLayer is not null && Layers.Count > 1;

    public void MoveLayer(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
            return;
        if (fromIndex < 0 || fromIndex >= Layers.Count)
            return;
        if (toIndex < 0 || toIndex >= Layers.Count)
            return;

        var item = Layers[fromIndex];
        Layers.RemoveAt(fromIndex);
        Layers.Insert(toIndex, item);
        SelectedLayer = item;
        IsDirty = true;
    }

    public void MoveLayer(LayerViewModel draggedLayer, LayerViewModel? targetLayer)
    {
        var from = Layers.IndexOf(draggedLayer);
        var to = targetLayer is null
            ? Layers.Count - 1
            : Layers.IndexOf(targetLayer);

        MoveLayer(from, to);
    }

    public bool HandleCanvasPointerPressed(Geometry.Point modelPoint, bool isLeftButtonPressed, bool isRightButtonPressed, KeyModifiers modifiers, double hitTolerance, IEnumerable<IFigure> figures, out bool shouldStartDragging)
    {
        shouldStartDragging = false;

        if (SelectedTool == ToolKind.Fill && isLeftButtonPressed)
        {
            var fillTarget = figures.Reverse().FirstOrDefault(f => f.IsIn(modelPoint, hitTolerance));
            if (fillTarget is not null && (fillTarget is Polygon || fillTarget is CurvedPolygon || fillTarget is Ellipse))
            {
                ToggleFigureFill(fillTarget);
                SelectedFigure = fillTarget;
            }
            return true;
        }

        if (SelectedTool == ToolKind.Line || SelectedTool == ToolKind.Polygon || SelectedTool == ToolKind.Ellipse || SelectedTool == ToolKind.Curve || SelectedTool == ToolKind.CurvedPolygon)
        {
            PressedPoints.Add(modelPoint);
            if (SelectedTool == ToolKind.Line && isLeftButtonPressed)
            {
                if (_lineStart is null)
                {
                    // При первом клике сохраняем начальную точку
                    _lineStart = modelPoint;
                }
                else
                {
                    // При втором клике создаём линию и сбрасываем точку
                    AddNewFigure(new Line(_lineStart, modelPoint));
                    _lineStart = null;
                }

                return true;
            }

            if (SelectedTool == ToolKind.Polygon)
            {
                if (isLeftButtonPressed)
                {
                    _polygonPoints.Add(modelPoint);
                    return true;
                }

                if (isRightButtonPressed && _polygonPoints.Count >= 3)
                {
                    AddNewFigure(new Polygon(_polygonPoints.ToArray()));
                    _polygonPoints.Clear();
                    return true;
                }
            }

            if (SelectedTool == ToolKind.Ellipse && isLeftButtonPressed)
            {
                if (_ellipseCenter is null)
                {
                    _ellipseCenter = modelPoint;
                }
                else
                {
                    var c = _ellipseCenter;
                    var rx = Math.Abs(modelPoint.X - c.X);
                    var ry = Math.Abs(modelPoint.Y - c.Y);
                    if (rx < 1) rx = 1;
                    if (ry < 1) ry = 1;

                    if (modifiers.HasFlag(KeyModifiers.Shift))
                        rx = ry = Math.Max(Math.Abs(rx), Math.Abs(ry));

                    AddNewFigure(new Ellipse(c, rx, ry));
                    _ellipseCenter = null;
                }

                return true;
            }

            if (SelectedTool == ToolKind.Curve)
            {
                if (isLeftButtonPressed)
                {
                    _curvePoints.Add(modelPoint);
                    if (_curvePoints.Count == 3)
                    {
                        AddNewFigure(new Curve(_curvePoints.ToArray()));
                        _curvePoints.Clear();
                    }
                    return true;
                }

                // отмена ввода кривой правой кнопкой
                if (isRightButtonPressed)
                {
                    _curvePoints.Clear();
                    return true;
                }
            }

            if (SelectedTool == ToolKind.CurvedPolygon)
            {
                if (isLeftButtonPressed)
                {
                    _curvedPolygonPoints.Add(modelPoint);
                    return true;
                }

                // завершение ввода правой кнопкой
                if (isRightButtonPressed)
                {
                    var count = _curvedPolygonPoints.Count;
                    if (count >= 4 && count % 2 == 0)
                    {
                        AddNewFigure(new CurvedPolygon(_curvedPolygonPoints.ToArray()));
                        _curvedPolygonPoints.Clear();
                        return true;
                    }
                }
            }
        }

        if (!isLeftButtonPressed)
            return false;

        var hit = figures.Reverse().FirstOrDefault(f => f.IsIn(modelPoint, hitTolerance));

        if (hit is null)
        {
            SelectedFigure = null;
            return true;
        }

        if (SelectedTool == ToolKind.Eraser)
        {
            if (CurrentLayerFigures.Contains(hit))
            {
                DeleteFigure(hit);
                if (ReferenceEquals(SelectedFigure, hit))
                    SelectedFigure = null;
                IsDirty = true;
            }

            return true;
        }

        SelectedFigure = hit;

        if (SelectedTool == ToolKind.Move)
        {
            shouldStartDragging = true;
            PreviousCenter.X = SelectedFigure.Center.X;
            PreviousCenter.Y = SelectedFigure.Center.Y;
        }

        return true;
    }

    public void HandleCanvasDragDelta(double dx, double dy)
    {
        if (SelectedFigure is null || SelectedTool != ToolKind.Move)
            return;

        SelectedFigure.Move(dx, dy);

        IsDirty = true;
    }

    public void HandleCanvasPointerReleased(double dx, double dy)
    {
        if (SelectedFigure is null || SelectedTool != ToolKind.Move)
            return;

        var newCenter = SelectedFigure.Center;
        newCenter.X += dx;
        newCenter.Y += dy;

        var command = new MoveFigureCommand(SelectedFigure, PreviousCenter, newCenter);
        ExecuteCommand(command);

        IsDirty = true;
    }

    private void ToggleFigureFill(IFigure figure)
    {
        var layer = Layers.FirstOrDefault(l => l.FiguresGraphicProperties.ContainsKey(figure));
        if (layer is null)
            return;

        var oldProps = layer.FiguresGraphicProperties[figure];
        var newIsFilled = !oldProps.IsFilled;
        var newFillColor = newIsFilled ? ActiveColor : oldProps.FillColor;

        var newProps = new FigureGraphicProperties(
            oldProps.Color,
            oldProps.Thickness,
            isFilled: newIsFilled,
            fillColor: newFillColor);

        ExecuteCommand(new ToggleFillFigureCommand(
            layer.FiguresGraphicProperties,
            VisibleFiguresGraphicProperties,
            figure,
            oldProps,
            newProps));
    }

    private void InitializeDemoFigures()
    {
        var line = new Line(new Geometry.Point(100, 120), new Geometry.Point(360, 180));
        AddFigureToCurrentLayer(line, new FigureGraphicProperties(Colors.CornflowerBlue, 2.0));

        var polygon = new Polygon(new[]
        {
            new Geometry.Point(420, 260),
            new Geometry.Point(540, 300),
            new Geometry.Point(520, 380),
            new Geometry.Point(430, 360),
        });
        AddFigureToCurrentLayer(polygon, new FigureGraphicProperties(Colors.Yellow, 2.0));

        var polygon2 = new Polygon(new[]
        {
            new Geometry.Point(470, 260),
            new Geometry.Point(590, 300),
            new Geometry.Point(570, 380),
            new Geometry.Point(480, 360),
        });
        AddFigureToCurrentLayer(polygon2, new FigureGraphicProperties(Colors.Red, 2.0));
    }

    private async Task<bool> ConfirmLoseChangesIfDirtyAsync()
    {
        if (!IsDirty)
            return true;

        return await _dialogs.ConfirmAsync(
            title: "Несохранённые изменения",
            message: "Есть несохранённые изменения. Вы уверены?");
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private void AddFigureToCurrentLayer(IFigure figure, IFigureGraphicProperties figureGraphicProperties)
    {
        CurrentLayerFiguresGraphicProperties[figure] = figureGraphicProperties;
        CurrentLayerFigures.Add(figure);
    }

    private void RemoveFigureFromCurrentLayer(IFigure figure)
    {
        CurrentLayerFiguresGraphicProperties.Remove(figure);
        CurrentLayerFigures.Remove(figure);
    }

    private void PushRecentColor(Color color)
    {
        if (RecentColors.Count > 0 && RecentColors[0] == color)
            return;

        for (var i = RecentColors.Count - 1; i >= 0; i--)
        {
            if (RecentColors[i] == color)
                RecentColors.RemoveAt(i);
        }

        RecentColors.Insert(0, color);
        const int max = 5;
        while (RecentColors.Count > max)
            RecentColors.RemoveAt(RecentColors.Count - 1);
    }
}