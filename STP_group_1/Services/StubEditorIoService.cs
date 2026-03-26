using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InputOutput;
using Geometry;

namespace STP_group_1.Services;

public sealed class StubEditorIoService : IEditorIoService
{
    public async Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures)
    {
        await FigureJsonIo.SaveFiguresAsync(figures, path);
    }

    public Task ExportFlatImageAsync(string path)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<IFigure>> OpenNativeProjectAsync(string path)
    {
        return await FigureJsonIo.LoadFiguresAsync(path);
    }
}