using Avalonia.Controls;
using STP_group_1.ViewModels.Dialogs;

namespace STP_group_1.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ConfirmDialogViewModel vm)
                vm.CloseRequested += CloseWithResult;
        };
    }

    private void CloseWithResult(bool result)
    {
        Close(result);
    }
}


