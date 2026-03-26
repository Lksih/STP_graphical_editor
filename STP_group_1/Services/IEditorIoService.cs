using Geometry;
using Geometry.Graphic;
using InputOutput;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STP_group_1.Services;

/// <summary>
/// Абстракция для IO-команды: UI не читает/пишет файлы напрямую и не знает о форматах.
/// Реализация должна жить в проекте IO (или адаптере поверх него).
/// </summary>
public interface IEditorIoService
{
    Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> styles);
    Task ExportFlatImageAsync(string filePath, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties); // PNG/JPG...
    public (IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles) ImportSVG(string filePath);
    Task<(IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles)> OpenNativeProjectAsync(string path);
}