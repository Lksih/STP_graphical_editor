using Geometry;
using Geometry.Graphic;
using InputOutput;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STP_group_1.Services;

public interface IEditorIoService
{
    Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> styles);
    void ExportSVGAndRaster(string filePath, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties, double canvasWidth, double canvasHeight);
    (IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles) ImportSVG(string filePath);
    Task<(IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles)> OpenNativeProjectAsync(string path);
}