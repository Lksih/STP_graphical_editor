using Avalonia.Controls;

namespace STP_group_1.Views.Dialogs;

public partial class NewCanvasDialog : Window
{
    public NewCanvasDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is NewCanvasDialogViewModel vm)
                vm.CloseRequested += ok => Close(ok);
        };
    }
}


