using Geometry;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Models;

namespace STP_group_1.ViewModels
{
    public interface IUndoRedoCommand
    {
        void Execute();
        void Undo();
        string Description { get; }
    }

    public class UndoRedoManager : ReactiveObject
    {
        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();
        private readonly int _maxStackSize = 100;

        private bool _isExecuting;
        private bool _isUndoing;

        private bool _canUndo;
        public bool CanUndo
        {
            get => _canUndo;
            private set => this.RaiseAndSetIfChanged(ref _canUndo, value);
        }

        private bool _canRedo;
        public bool CanRedo
        {
            get => _canRedo;
            private set => this.RaiseAndSetIfChanged(ref _canRedo, value);
        }

        private string _undoDescription = "";
        public string UndoDescription
        {
            get => _undoDescription;
            private set => this.RaiseAndSetIfChanged(ref _undoDescription, value);
        }

        private string _redoDescription = "";
        public string RedoDescription
        {
            get => _redoDescription;
            private set => this.RaiseAndSetIfChanged(ref _redoDescription, value);
        }

        public event EventHandler<(IUndoRedoCommand command, bool isUndo)>? CommandExecuted;

        public void ExecuteCommand(IUndoRedoCommand command)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            try
            {
                command.Execute();
                _undoStack.Push(command);
                _redoStack.Clear();

                while (_undoStack.Count > _maxStackSize)
                    _undoStack.Pop();

                UpdateCanExecute();

                CommandExecuted?.Invoke(this, (command, false));
            }
            finally
            {
                _isExecuting = false;
            }
        }

        public void Undo()
        {
            if (_isUndoing || _undoStack.Count == 0) return;

            _isUndoing = true;
            try
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                UpdateCanExecute();

                CommandExecuted?.Invoke(this, (command, true));
            }
            finally
            {
                _isUndoing = false;
            }
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            var command = _redoStack.Pop();

            command.Execute();
            _undoStack.Push(command);

            UpdateCanExecute();
            CommandExecuted?.Invoke(this, (command, false));
        }

        private void UpdateCanExecute()
        {
            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;

            UndoDescription = _undoStack.Count > 0 ? _undoStack.Peek().Description : "";
            RedoDescription = _redoStack.Count > 0 ? _redoStack.Peek().Description : "";
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateCanExecute();
        }
    }

    public class AddFigureCommand : IUndoRedoCommand
    {
        private readonly System.Collections.ObjectModel.ObservableCollection<IFigure> _figures;
        private readonly IFigure _figure;

        public AddFigureCommand(System.Collections.ObjectModel.ObservableCollection<IFigure> figures, IFigure figure)
        {
            _figures = figures;
            _figure = figure;
        }

        public string Description => $"Добавить {_figure.GetType().Name}";

        public void Execute()
        {
            _figures.Add(_figure);
        }

        public void Undo()
        {
            _figures.Remove(_figure);
        }
    }

    public class MoveFigureCommand : IUndoRedoCommand
    {
        private readonly IFigure _figure;
        private readonly double _dx;
        private readonly double _dy;

        public MoveFigureCommand(IFigure figure, double dx, double dy)
        {
            _figure = figure;
            _dx = dx;
            _dy = dy;
        }

        public string Description => "Переместить фигуру";

        public void Execute()
        {
            _figure.Move(_dx, _dy);
        }

        public void Undo()
        {
            _figure.Move(-_dx, -_dy);
        }
    }

    public class DeleteFigureCommand : IUndoRedoCommand
    {
        private readonly System.Collections.ObjectModel.ObservableCollection<IFigure> _figures;
        Dictionary<IFigure, IFigureGraphicProperties> _figuresGraphicProperties;
        IFigureGraphicProperties _currentFigureGraphicProperties;
        private readonly IFigure _figure;
        private readonly int _index;
        private readonly MainWindowViewModel? _viewModel;
        public DeleteFigureCommand(System.Collections.ObjectModel.ObservableCollection<IFigure> figures, Dictionary<IFigure, IFigureGraphicProperties> figuresGraphicProperties, IFigure figure)
        {
            _figures = figures;
            _figure = figure;
            _figuresGraphicProperties = figuresGraphicProperties;
            _currentFigureGraphicProperties = _figuresGraphicProperties[_figure];
            _index = figures.IndexOf(figure);
        }

        public string Description => "Удалить фигуру";

        public void Execute()
        {
            _figuresGraphicProperties.Remove(_figure);
            _figures.Remove(_figure);
        }

        public void Undo()
        {
            _figuresGraphicProperties[_figure] = _currentFigureGraphicProperties;
            if (_index >= 0 && _index <= _figures.Count)
                _figures.Insert(_index, _figure);
            else
                _figures.Add(_figure);
        }
    }

    public class RotateFigureCommand : IUndoRedoCommand
    {
        private readonly IFigure _figure;
        private readonly double _angle;

        public RotateFigureCommand(IFigure figure, double angle)
        {
            _figure = figure;
            _angle = angle;
        }

        public string Description => "Повернуть фигуру";

        public void Execute()
        {
            _figure.Rotate(_angle);
        }

        public void Undo()
        {
            _figure.Rotate(-_angle);
        }
    }

    public class CompositeCommand : IUndoRedoCommand
    {
        private readonly List<IUndoRedoCommand> _commands = new();

        public CompositeCommand(params IUndoRedoCommand[] commands)
        {
            _commands.AddRange(commands);
        }

        public CompositeCommand(IEnumerable<IUndoRedoCommand> commands)
        {
            _commands.AddRange(commands);
        }

        public string Description => "Составное действие";

        public void Execute()
        {
            foreach (var cmd in _commands)
                cmd.Execute();
        }

        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
                _commands[i].Undo();
        }

        public void AddCommand(IUndoRedoCommand command)
        {
            _commands.Add(command);
        }
    }
}