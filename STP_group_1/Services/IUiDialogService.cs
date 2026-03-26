using System.Threading.Tasks;
using Avalonia.Media;

namespace STP_group_1.Services;

public readonly record struct NewCanvasOptions(double Width, double Height, Color Background);

public interface IUiDialogService
{
    Task<bool> ConfirmAsync(string title, string message);

    Task<NewCanvasOptions?> ShowNewCanvasDialogAsync(double currentWidth, double currentHeight, Color currentBackground);

    Task<string?> PickOpenFileAsync(string[] extensions);

    Task<string?> PickSaveFileAsync(string defaultExtension, string suggestedFileName);
}


