using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using STP_group_1.ViewModels;

namespace STP_group_1.Views
{
    public partial class MainWindow : Window
    {
        private bool _closeConfirmed;
        private LayerViewModel? _dragLayer;

        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            var list = this.FindControl<ListBox>("LayersList");
            if (list is null)
                return;

            list.AddHandler(DragDrop.DropEvent, OnLayersDrop);
            list.AddHandler(DragDrop.DragOverEvent, OnLayersDragOver);
            list.AddHandler(PointerPressedEvent, OnLayersPointerPressed, RoutingStrategies.Tunnel);
        }

        private async void OnLayersPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox list)
                return;

            var point = e.GetCurrentPoint(list);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            var item = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();
            if (item?.DataContext is not LayerViewModel layer)
                return;

            _dragLayer = layer;

#pragma warning disable CS0618
            var data = new DataObject();
            data.Set("layer", layer);

            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618
        }

        private void OnLayersDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains("layer") ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnLayersDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox list)
                return;
            if (!e.Data.Contains("layer"))
                return;

            var dragged = e.Data.Get("layer") as LayerViewModel ?? _dragLayer;
            if (dragged is null)
                return;

            var targetItem = (e.Source as Control)?.FindAncestorOfType<ListBoxItem>();
            var targetLayer = targetItem?.DataContext as LayerViewModel;

            if (DataContext is not MainWindowViewModel vm)
                return;

            var from = vm.Layers.IndexOf(dragged);
            var to = targetLayer is null ? vm.Layers.Count - 1 : vm.Layers.IndexOf(targetLayer);
            if (from < 0 || to < 0)
                return;

            vm.MoveLayer(from, to);
            e.Handled = true;
        }

        private async void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_closeConfirmed)
                return;

            if (DataContext is not MainWindowViewModel vm)
                return;

            e.Cancel = true;
            if (await vm.CanCloseAsync())
            {
                _closeConfirmed = true;
                Close();
            }
        }
    }
}