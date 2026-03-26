using Geometry;
using Geometry.Graphic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STP_group_1.Services;

/// <summary>
/// Абстракция для IO-команды: UI не читает/пишет файлы напрямую и не знает о форматах.
/// Реализация должна жить в проекте IO (или адаптере поверх него).
/// </summary>
public interface IEditorIoService
{
    Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures);
    Task ExportFlatImageAsync(string filePath, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties); // PNG/JPG...
    Task<IReadOnlyList<IFigure>> OpenNativeProjectAsync(string path);
}