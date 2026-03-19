using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Geometry;
using ReactiveUI;
using STP_group_1.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using UI.Models;

namespace STP_group_1.ViewModels;

public enum ToolKind
{
    Move,
    Eraser,
    Line,
    Polygon,
    Ellipse,
}

public enum ActiveColorTarget
{
    Foreground,
    Background
}



public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IUiDialogService _dialogs;
    private readonly IEditorIoService _io;

    private bool _isColorPickerVisible;
    public bool IsColorPickerVisible
    {
        get => _isColorPickerVisible;
        set => this.RaiseAndSetIfChanged(ref _isColorPickerVisible, value);
    }

    public MainWindowViewModel(IUiDialogService dialogs, IEditorIoService io)
    {
        _dialogs = dialogs;
        _io = io;

        // Initial state
        var initialLayer = new LayerViewModel { Name = "Background", PreviewBrush = Brushes.White };
        AttachLayer(initialLayer);
        Layers.Add(initialLayer);
        SelectedLayer = Layers.FirstOrDefault();
        SelectedTool = ToolKind.Move;
        SelectedTheme = EditorTheme.System;

        // ---- Derived properties (ReactiveUI way) ----

        this.WhenAnyValue(x => x.SelectedTool)
            .Select(t => t.ToString())
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

        this.WhenAnyValue(x => x.ZoomPercent)
            .Select(z => z / 100.0)
            .ToProperty(this, x => x.ZoomFactor, out _zoomFactor);

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

        // Apply theme when it changes
        this.WhenAnyValue(x => x.SelectedTheme)
            .Skip(1)
            .Subscribe(ApplyTheme);

        RecentColors.CollectionChanged += (_, __) => this.RaisePropertyChanged(nameof(HasRecentColors));

        Layers.CollectionChanged += (_, __) => RaiseLayersChanged();

        // ---- Commands ----

        SelectToolCommand = ReactiveCommand.Create<string?>(SelectTool);

        NewCommand = ReactiveCommand.CreateFromTask(New);
        OpenCommand = ReactiveCommand.CreateFromTask(Open);
        SaveCommand = ReactiveCommand.CreateFromTask(Save);
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        ExitCommand = ReactiveCommand.CreateFromTask(Exit);


        UndoCommand = ReactiveCommand.Create(Undo);
        RedoCommand = ReactiveCommand.Create(Redo);

        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);

        DeleteSelectedFigureCommand = ReactiveCommand.Create(DeleteSelectedFigure);
        RotateSelectedFigureCommand = ReactiveCommand.Create(RotateSelectedFigure);

        AddLineCommand = ReactiveCommand.Create(AddLine);
        AddPolygonCommand = ReactiveCommand.Create(AddPolygon);
        AddEllipseCommand = ReactiveCommand.Create(AddEllipse);

        SelectThemeCommand = ReactiveCommand.Create<string?>(SelectTheme);

        NewLayerCommand = ReactiveCommand.Create(NewLayer);

        SelectActiveColorTargetCommand = ReactiveCommand.Create<string?>(SelectActiveColorTarget);

        var canDeleteLayer = this.WhenAnyValue(x => x.SelectedLayer)
            .Select(_ => CanDeleteLayer());

        DeleteLayerCommand = ReactiveCommand.Create(DeleteLayer, canDeleteLayer);

        ApplyRecentColorCommand = ReactiveCommand.Create<object?>(ApplyRecentColor);

        InitializeDemoFigures();
    }

    // ---------- Document state ----------

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

    // ---------- Canvas ----------

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

    // ---------- Tools ----------

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
    public bool IsForegroundActive => ActiveColorTarget == ActiveColorTarget.Foreground;
    public bool IsCanvasBackgroundActive => ActiveColorTarget == ActiveColorTarget.Background;

    // ---------- Tool properties ----------

    private Color _foregroundColor = Colors.Black;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set => this.RaiseAndSetIfChanged(ref _foregroundColor, value);
    }

    private int _brushSize = 16;
    public int BrushSize
    {
        get => _brushSize;
        set => this.RaiseAndSetIfChanged(ref _brushSize, value);
    }

    private int _opacityPercent = 100;
    public int OpacityPercent
    {
        get => _opacityPercent;
        set => this.RaiseAndSetIfChanged(ref _opacityPercent, value);
    }

    private int _hardnessPercent = 80;
    public int HardnessPercent
    {
        get => _hardnessPercent;
        set => this.RaiseAndSetIfChanged(ref _hardnessPercent, value);
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
                // ignore invalid strings
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

    public bool HasRecentColors => RecentColors.Count > 0;

    // ---------- Zoom ----------

    private int _zoomPercent = 100;
    public int ZoomPercent
    {
        get => _zoomPercent;
        set => this.RaiseAndSetIfChanged(ref _zoomPercent, Clamp(value, 10, 800));
    }

    private readonly ObservableAsPropertyHelper<double> _zoomFactor;
    public double ZoomFactor => _zoomFactor.Value;

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

    // ---------- Theme ----------

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


    // ---------- Layers ----------

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

        foreach (var layer in Layers.Where(l => l.IsVisible))
        {
            foreach (var figure in layer.Figures)
                VisibleFigures.Add(figure);
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

    // ---------- Geometry CurrentLayerFigures (per-layer) ----------

    private static readonly ObservableCollection<IFigure> EmptyFigures = new();

    /// <summary>
    /// Фигуры активного слоя. Для невырбранного слоя возвращается пустая коллекция.
    /// </summary>
    public ObservableCollection<IFigure> CurrentLayerFigures => SelectedLayer?.Figures ?? EmptyFigures;

    private IFigure? _selectedFigure;
    public IFigure? SelectedFigure
    {
        get => _selectedFigure;
        set => this.RaiseAndSetIfChanged(ref _selectedFigure, value);
    }

    // ---------- Commands (public for XAML bindings) ----------

    public ReactiveCommand<string?, Unit> SelectToolCommand { get; }

    public ReactiveCommand<Unit, Unit> NewCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }

    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteSelectedFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> RotateSelectedFigureCommand { get; }

    public ReactiveCommand<Unit, Unit> AddLineCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPolygonCommand { get; }
    public ReactiveCommand<Unit, Unit> AddEllipseCommand { get; }

    public ReactiveCommand<string?, Unit> SelectThemeCommand { get; }

    public ReactiveCommand<Unit, Unit> NewLayerCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteLayerCommand { get; }

    public ReactiveCommand<string?, Unit> SelectActiveColorTargetCommand { get; }

    public ReactiveCommand<object?, Unit> ApplyRecentColorCommand { get; }


    // ---------- Command handlers ----------

    private void SelectTool(string? tool)
    {
        if (Enum.TryParse<ToolKind>(tool, ignoreCase: true, out var parsed))
            SelectedTool = parsed;
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

        var path = await _dialogs.PickOpenFileAsync(new[] { ".png", ".jpg", ".jpeg" });
        if (path is null)
            return;

        await _io.OpenFlatImageAsync(path);

        CurrentProjectPath = path;
        IsDirty = false;
    }

    private async Task Save()
    {
        var path = CurrentProjectPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = await _dialogs.PickSaveFileAsync(defaultExtension: ".graphify", suggestedFileName: "project.graphify");
            if (path is null)
                return;
            CurrentProjectPath = path;
        }

        await _io.SaveNativeProjectAsync(path);
        IsDirty = false;
    }

    private async Task Export()
    {
        var path = await _dialogs.PickSaveFileAsync(defaultExtension: ".png", suggestedFileName: "export.png");
        if (path is null)
            return;

        await _io.ExportFlatImageAsync(path);
    }

    private async Task Exit()
    {
        if (!await ConfirmLoseChangesIfDirtyAsync())
            return;

        _dialogs.RequestCloseMainWindow();
    }

    public Task<bool> CanCloseAsync() => ConfirmLoseChangesIfDirtyAsync();

    private void Undo()
    {
        // TODO: connect to real undo stack from application/core.
    }

    private void Redo()
    {
        // TODO: connect to real undo stack from application/core.
    }

    private void ZoomIn() => ZoomPercent = Clamp(ZoomPercent + 10, 10, 800);

    private void ZoomOut() => ZoomPercent = Clamp(ZoomPercent - 10, 10, 800);


    private void DeleteSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        CurrentLayerFigures.Remove(SelectedFigure);
        SelectedFigure = null;
        IsDirty = true;
    }

    private void RotateSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        // Поворот на 15 градусов (в радианах)
        const double stepDegrees = 15.0;
        var angle = Math.PI * stepDegrees / 180.0;

        SelectedFigure.Rotate(angle);
        IsDirty = true;
    }

    private void AddLine()
    {
        var cx = CanvasWidth / 2.0;
        var cy = CanvasHeight / 2.0;

        var a = new Geometry.Point(cx - 100, cy);
        var b = new Geometry.Point(cx + 100, cy);

        var fig = new GraphicLine(a, b, ForegroundColor, 2.0);

        CurrentLayerFigures.Add(fig);
        SelectedFigure = fig;
        IsDirty = true;
    }

    private void AddPolygon()
    {
        var cx = CanvasWidth / 2.0;
        var cy = CanvasHeight / 2.0;

        var verts = new[]
        {
            new Geometry.Point(cx, cy - 80),
            new Geometry.Point(cx + 80, cy + 40),
            new Geometry.Point(cx, cy + 80),
            new Geometry.Point(cx - 80, cy + 40),
        };

        var fig = new GraphicPolygon(verts, ForegroundColor, 2.0);

        CurrentLayerFigures.Add(fig);
        SelectedFigure = fig;
        IsDirty = true;
    }

    private void AddEllipse()
    {
        var cx = CanvasWidth / 2.0;
        var cy = CanvasHeight / 2.0;

        var fig = new GraphicEllipse(new Geometry.Point(cx, cy), 120, 80, ForegroundColor, 2.0);

        CurrentLayerFigures.Add(fig);
        SelectedFigure = fig;
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

    // ---------- Helpers ----------

    private void InitializeDemoFigures()
    {
        // Заготовка на базе Geometry.IFigure
        CurrentLayerFigures.Add(new GraphicLine(new Geometry.Point(100, 120), new Geometry.Point(360, 180),
            Colors.CornflowerBlue,
            2.0
        ));

        CurrentLayerFigures.Add(new GraphicPolygon(new[]
        {
            new Geometry.Point(420, 260),
            new Geometry.Point(540, 300),
            new Geometry.Point(520, 380),
            new Geometry.Point(430, 360),
        },
            Colors.OrangeRed,
            2.0
        ));
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
