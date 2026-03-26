using Geometry;
using Geometry.Graphic;
using InputOutput;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STP_group_1.Services;

public sealed class StubEditorIoService : IEditorIoService
{
    public async Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures)
    {
        await FigureJsonIo.SaveFiguresAsync(figures, path);
    }

    public Task ExportFlatImageAsync(string filePath, IEnumerable<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties)
    {
        SVGConverter.Save(figures, figuresGraphicProperties, filePath, 500, 500);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IFigure>> OpenNativeProjectAsync(string path)
    {
        return await FigureJsonIo.LoadFiguresAsync(path);
    }
}