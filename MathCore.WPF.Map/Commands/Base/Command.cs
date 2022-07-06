#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MathCore.WPF.Map.Commands.Base;

public abstract class Command : ICommand, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null!) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

    protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null!)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(PropertyName);
        return true;
    }

    private readonly List<WeakReference<EventHandler>> _EventHandlers = new();
    event EventHandler? ICommand.CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
            if (value is not null)
                _EventHandlers.Add(new(value));
        }
        remove
        {
            CommandManager.RequerySuggested -= value;

            if (value?.Target is not { } target) return;
            for (var i = 0; i < _EventHandlers.Count; i++)
            {
                var wref = _EventHandlers[i];
                if (wref.TryGetTarget(out var handler) && !ReferenceEquals(handler, value)) 
                    continue;

                _EventHandlers.Remove(wref);
                i--;
            }
        }
    }

    public void InvokeCanExecuteChanged()
    {
        for (var i = 0; i < _EventHandlers.Count; i++)
        {
            var wref = _EventHandlers[i];
            if (!wref.TryGetTarget(out var handler))
            {
                _EventHandlers.Remove(wref);
                i--;
                continue;
            }

            handler(this, EventArgs.Empty);
        }
    }

    bool ICommand.CanExecute(object? parameter) => CanExecute(parameter);

    void ICommand.Execute(object? parameter)
    {
        if (CanExecute(parameter))
            Execute(parameter);
    }

    private bool _IsCanExecute = true;

    public bool IsCanExecute { get => _IsCanExecute; set => Set(ref _IsCanExecute, value); }

    protected virtual bool CanExecute(object? p) => _IsCanExecute;

    protected abstract void Execute(object? p);
}
