using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InputOutput;
using Geometry;

namespace STP_group_1.Services;

/// <summary>
/// Временная заглушка: чтобы UI уже был рабочим (меню/диалоги/команды),
/// но реальная сериализация/десериализация остаётся за командой IO.
/// </summary>
public sealed class StubEditorIoService : IEditorIoService
{
    public Task OpenFlatImageAsync(string path)
        => Task.CompletedTask;

    public Task SaveNativeProjectAsync(string path, IEnumerable<IFigure> figures)
    {
        FigureJsonIo.SaveFigures(figures, path);
        return Task.CompletedTask;
    }

    public Task ExportFlatImageAsync(string path)
        => Task.CompletedTask;

    public Task<IReadOnlyList<IFigure>> OpenNativeProjectAsync(string path)
    {
        return Task.FromResult(FigureJsonIo.LoadFigures(path));
    }
}