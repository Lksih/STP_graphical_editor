using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using DynamicData;
using Geometry;
using ReactiveUI;
using STP_group_1.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IUiDialogService _dialogs;
    private readonly IEditorIoService _io;

    public MainWindowViewModel(IUiDialogService dialogs, IEditorIoService io)
    {
        _dialogs = dialogs;
        _io = io;

        // Initial state
        Layers.Add(new LayerViewModel { Name = "Background", PreviewBrush = Brushes.White });
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
            .Select(z => $"{z}%")
            .ToProperty(this, x => x.ZoomPercentText, out _zoomPercentText);

        this.WhenAnyValue(x => x.CanvasWidth, x => x.ZoomFactor)
            .Select(t => t.Item1 * t.Item2)
            .ToProperty(this, x => x.CanvasWidthZoomed, out _canvasWidthZoomed);

        this.WhenAnyValue(x => x.CanvasHeight, x => x.ZoomFactor)
            .Select(t => t.Item1 * t.Item2)
            .ToProperty(this, x => x.CanvasHeightZoomed, out _canvasHeightZoomed);

        // Track recent colors
        this.WhenAnyValue(x => x.ForegroundColor)
            .Skip(1)
            .Subscribe(PushRecentColor);

        this.WhenAnyValue(x => x.BackgroundColor)
            .Skip(1)
            .Subscribe(PushRecentColor);

        // Apply theme when it changes
        this.WhenAnyValue(x => x.SelectedTheme)
            .Skip(1)
            .Subscribe(ApplyTheme);

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

        SwapColorsCommand = ReactiveCommand.Create(SwapColors);
        SetForegroundFromRecentCommand = ReactiveCommand.Create<object?>(SetForegroundFromRecent);
        SetBackgroundFromRecentCommand = ReactiveCommand.Create<object?>(SetBackgroundFromRecent);

        DeleteSelectedFigureCommand = ReactiveCommand.Create(DeleteSelectedFigure);
        RotateSelectedFigureCommand = ReactiveCommand.Create(RotateSelectedFigure);

        AddLineCommand = ReactiveCommand.Create(AddLine);
        AddPolygonCommand = ReactiveCommand.Create(AddPolygon);
        AddEllipseCommand = ReactiveCommand.Create(AddEllipse);

        SelectThemeCommand = ReactiveCommand.Create<string?>(SelectTheme);

        NewLayerCommand = ReactiveCommand.Create(NewLayer);

        var canDeleteLayer = this.WhenAnyValue(x => x.SelectedLayer)
            .Select(_ => CanDeleteLayer());

        DeleteLayerCommand = ReactiveCommand.Create(DeleteLayer, canDeleteLayer);

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

    // ---------- Tool properties ----------

    private Color _foregroundColor = Colors.Black;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set => this.RaiseAndSetIfChanged(ref _foregroundColor, value);
    }

    private Color _backgroundColor = Colors.White;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => this.RaiseAndSetIfChanged(ref _backgroundColor, value);
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

    public ObservableCollection<Color> RecentColors { get; } = new();

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

    private readonly ObservableAsPropertyHelper<string> _zoomPercentText;
    public string ZoomPercentText => _zoomPercentText.Value;

    // If you ever decide to allow editing ZoomPercentText from UI with parsing,
    // make it a separate "input" property (string) and parse into ZoomPercent.
    // Right now your XAML binds Text="{Binding ZoomPercentText}" (TwoWay by default for TextBox),
    // but this property is computed. It worked in Toolkit only because you had a manual setter.
    // Чтобы не ломать XAML — см. ниже workaround: отдельная команда ParseZoomTextCommand либо отдельное свойство.
    // 
    // На практике лучше в XAML указать Mode=OneWay для ZoomPercentText,
    // но ты просил не менять стиль окна, поэтому здесь мы оставим computed,
    // а редактирование текста можно вернуть отдельным "input" свойством при необходимости.

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
            this.RaisePropertyChanged(nameof(Figures));
        }
    }

    // ---------- Geometry figures (per-layer) ----------

    private static readonly ObservableCollection<IFigure> EmptyFigures = new();

    /// <summary>
    /// Фигуры активного слоя. Для невырбранного слоя возвращается пустая коллекция.
    /// </summary>
    public ObservableCollection<IFigure> Figures => SelectedLayer?.Figures ?? EmptyFigures;

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

    public ReactiveCommand<Unit, Unit> SwapColorsCommand { get; }
    public ReactiveCommand<object?, Unit> SetForegroundFromRecentCommand { get; }
    public ReactiveCommand<object?, Unit> SetBackgroundFromRecentCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteSelectedFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> RotateSelectedFigureCommand { get; }

    public ReactiveCommand<Unit, Unit> AddLineCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPolygonCommand { get; }
    public ReactiveCommand<Unit, Unit> AddEllipseCommand { get; }

    public ReactiveCommand<string?, Unit> SelectThemeCommand { get; }

    public ReactiveCommand<Unit, Unit> NewLayerCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteLayerCommand { get; }

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
        Layers.Add(new LayerViewModel { Name = "Layer 1", PreviewBrush = new SolidColorBrush(CanvasBackground) });
        SelectedLayer = Layers.FirstOrDefault();

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

    private void SwapColors()
    {
        (ForegroundColor, BackgroundColor) = (BackgroundColor, ForegroundColor);
    }

    private void DeleteSelectedFigure()
    {
        if (SelectedFigure is null)
            return;

        Figures.Remove(SelectedFigure);
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

        var fig = new GraphicLine(a, b,
            System.Drawing.Color.FromArgb(ForegroundColor.A, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B),
            2.0
        );

        Figures.Add(fig);
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

        var fig = new GraphicPolygon(verts,
            System.Drawing.Color.FromArgb(ForegroundColor.A, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B),
            2.0
        );

        Figures.Add(fig);
        SelectedFigure = fig;
        IsDirty = true;
    }

    private void AddEllipse()
    {
        var cx = CanvasWidth / 2.0;
        var cy = CanvasHeight / 2.0;

        var fig = new GraphicEllipse(new Geometry.Point(cx, cy), 120, 80,
            System.Drawing.Color.FromArgb(ForegroundColor.A, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B),
            2.0
        );

        Figures.Add(fig);
        SelectedFigure = fig;
        IsDirty = true;
    }

    private void SetForegroundFromRecent(object? value)
    {
        if (value is Color c)
        {
            ForegroundColor = c;
            return;
        }

        if (value is string s)
        {
            try
            {
                ForegroundColor = Color.Parse(s);
            }
            catch
            {
                // ignore invalid strings
            }
        }
    }

    private void SetBackgroundFromRecent(object? value)
    {
        if (value is Color c)
        {
            BackgroundColor = c;
            return;
        }

        if (value is string s)
        {
            try
            {
                BackgroundColor = Color.Parse(s);
            }
            catch
            {
                // ignore invalid strings
            }
        }
    }

    private void SelectTheme(string? theme)
    {
        if (Enum.TryParse<EditorTheme>(theme, ignoreCase: true, out var parsed))
            SelectedTheme = parsed;
    }

    private void NewLayer()
    {
        Layers.Add(new LayerViewModel { Name = $"Layer {Layers.Count + 1}", PreviewBrush = Brushes.LightGray });
        SelectedLayer = Layers.LastOrDefault();
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
        Figures.Add(new GraphicLine(new Geometry.Point(100, 120), new Geometry.Point(360, 180),
            System.Drawing.Color.CornflowerBlue,
            2.0
        ));

        Figures.Add(new GraphicPolygon(new[]
        {
            new Geometry.Point(420, 260),
            new Geometry.Point(540, 300),
            new Geometry.Point(520, 380),
            new Geometry.Point(430, 360),
        },
            System.Drawing.Color.OrangeRed,
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
        const int max = 12;
        while (RecentColors.Count > max)
            RecentColors.RemoveAt(RecentColors.Count - 1);
    }
}
