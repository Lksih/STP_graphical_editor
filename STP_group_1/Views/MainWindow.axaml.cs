using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using STP_group_1.ViewModels;

namespace STP_group_1.Views
{
    public partial class MainWindow : Window
    {
        private bool _closeConfirmed;

        private LayerViewModel? _dragLayer;
        private Point? _dragStartPoint;
        private bool _isDraggingLayer;

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
        }

        private void OnLayerDragHandlePressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            var source = e.Source as Control;
            var item = source?.FindAncestorOfType<ListBoxItem>();
            if (item?.DataContext is not LayerViewModel layer)
                return;

            _dragLayer = layer;
            _dragStartPoint = e.GetPosition(this);
            _isDraggingLayer = false;

            e.Handled = true;
        }

        private async void OnLayerDragHandleMoved(object? sender, PointerEventArgs e)
        {
            if (_dragLayer is null || _dragStartPoint is null || _isDraggingLayer)
                return;

            var current = e.GetPosition(this);
            var dx = Math.Abs(current.X - _dragStartPoint.Value.X);
            var dy = Math.Abs(current.Y - _dragStartPoint.Value.Y);

            if (dx < 4 && dy < 4)
                return;

            _isDraggingLayer = true;

#pragma warning disable CS0618
            var data = new DataObject();
            data.Set("layer", _dragLayer);

            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
#pragma warning restore CS0618

            _dragLayer = null;
            _dragStartPoint = null;
            _isDraggingLayer = false;

            e.Handled = true;
        }

        private void OnLayerDragHandleReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragLayer = null;
            _dragStartPoint = null;
            _isDraggingLayer = false;
        }

        private void OnLayersDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains("layer")
                ? DragDropEffects.Move
                : DragDropEffects.None;

            e.Handled = true;
        }

        private void OnLayersDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox)
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

            vm.MoveLayer(dragged, targetLayer);
            e.Handled = true;
        }

        private void OnExitMenuClick(object? sender, RoutedEventArgs e)
        {
            // Запустит OnClosing
            Close();
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