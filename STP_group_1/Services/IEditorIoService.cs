using System.Threading.Tasks;

namespace STP_group_1.Services;

/// <summary>
/// Абстракция для IO-команды: UI не читает/пишет файлы напрямую и не знает о форматах.
/// Реализация должна жить в проекте IO (или адаптере поверх него).
/// </summary>
public interface IEditorIoService
{
    Task OpenFlatImageAsync(string path);       // PNG/JPG...
    Task SaveNativeProjectAsync(string path);   // нативный формат со слоями
    Task ExportFlatImageAsync(string path);     // PNG/JPG...
}


