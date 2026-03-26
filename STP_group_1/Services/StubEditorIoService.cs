using Geometry;
using Geometry.Graphic;
using InputOutput;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STP_group_1.Services;

public sealed class StubEditorIoService : IEditorIoService
{
    public async Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> styles)
    {
        await FigureJsonIo.SaveFiguresAsync(figures, styles, path);
    }

    public Task ExportFlatImageAsync(string filePath, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties)
    {
        SVGConverter.Save(figures, figuresGraphicProperties, filePath, 500, 500);
        return Task.CompletedTask;
    }

    public (IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles) ImportSVG(string filePath)
    {
        return SVGConverter.Load(filePath);
    }

    public async Task<(IReadOnlyList<IFigure> Figures, Dictionary<IFigure, IFigureGraphicProperties> Styles)> OpenNativeProjectAsync(string path)
    {
        return await FigureJsonIo.LoadFiguresAsync(path);
    }
}