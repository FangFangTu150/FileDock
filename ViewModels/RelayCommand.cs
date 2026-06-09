using System.Windows.Input;

namespace FileDock.ViewModels;

public interface IResultCommand<in T> : ICommand
{
    bool TryExecute(T? parameter);
}

public sealed class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class ResultRelayCommand<T>(Func<T?, bool> execute, Predicate<T?>? canExecute = null) : IResultCommand<T>
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _ = TryExecute((T?)parameter);

    public bool TryExecute(T? parameter)
    {
        return CanExecute(parameter) && execute(parameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
