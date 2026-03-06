using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using STP_group_1.Services;
using STP_group_1.ViewModels;
using STP_group_1.Views;

namespace STP_group_1
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            DisableAvaloniaDataAnnotationValidation();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();

                var dialogService = new AvaloniaDialogService(mainWindow);
                var ioService = new StubEditorIoService();
                var viewmodel= new MainWindowViewModel(dialogService, ioService);

                viewmodel.PickOpenFile.RegisterHandler(async s =>
                {
                    if(s.IsHandled) return;
                 var res= await dialogService.PickOpenFileAsync(s.Input);
                 if(res != null) {s.SetOutput(res); return;}
                });

                mainWindow.DataContext = new MainWindowViewModel(dialogService, ioService);
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}
