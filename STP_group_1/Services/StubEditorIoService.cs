using System;
using System.Threading.Tasks;

namespace STP_group_1.Services;

/// <summary>
/// Временная заглушка: чтобы UI уже был рабочим (меню/диалоги/команды),
/// но реальная сериализация/десериализация остаётся за командой IO.
/// </summary>
public sealed class StubEditorIoService : IEditorIoService
{
    public Task OpenFlatImageAsync(string path)
        => Task.CompletedTask;

    public Task SaveNativeProjectAsync(string path)
        => Task.CompletedTask;

    public Task ExportFlatImageAsync(string path)
        => Task.CompletedTask;
}


