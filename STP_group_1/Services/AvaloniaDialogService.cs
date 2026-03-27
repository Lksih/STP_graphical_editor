using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using STP_group_1.Views.Dialogs;
using STP_group_1.ViewModels.Dialogs;

namespace STP_group_1.Services;

public sealed class AvaloniaDialogService : IUiDialogService
{
    private readonly Window _owner;

    public AvaloniaDialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ConfirmDialog
        {
            Title = title,
            DataContext = new ConfirmDialogViewModel(message)
        };

        var result = await dialog.ShowDialog<bool?>(_owner);
        return result == true;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 560,
            Height = 450,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        };

        var okButton = new Button
        {
            Content = "OK",
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 88
        };
        okButton.Click += (_, _) => dialog.Close();

        var panel = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(okButton, Dock.Bottom);
        panel.Children.Add(okButton);
        panel.Children.Add(text);

        dialog.Content = panel;
        await dialog.ShowDialog(_owner);
    }

    public async Task<NewCanvasOptions?> ShowNewCanvasDialogAsync(double currentWidth, double currentHeight, Color currentBackground)
    {
        var vm = new NewCanvasDialogViewModel(currentWidth, currentHeight, currentBackground);
        var dialog = new NewCanvasDialog { DataContext = vm };

        var ok = await dialog.ShowDialog<bool?>(_owner);
        if (ok != true)
            return null;

        return new NewCanvasOptions(vm.Width, vm.Height, vm.Background);
    }

    public async Task<string?> PickOpenFileAsync(string[] extensions)
    {
        var sp = _owner.StorageProvider;
        if (sp is null)
            return null;

        var patterns = extensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.StartsWith('.') ? $"*{e}" : $"*.{e}")
            .ToArray();

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Images") { Patterns = patterns }
            }
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickSaveFileAsync(string defaultExtension, string suggestedFileName)
    {
        var sp = _owner.StorageProvider;
        if (sp is null)
            return null;

        var ext = defaultExtension.StartsWith('.') ? defaultExtension : "." + defaultExtension;
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ext.TrimStart('.'),
        });

        return file?.TryGetLocalPath();
    }
}