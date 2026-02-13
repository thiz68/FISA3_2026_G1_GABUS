namespace EasySave.WPF.Commands;

using System.Windows.Input;

// Implementation of ICommand for MVVM pattern
// Allows binding commands from ViewModels to Views
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    // Event raised when CanExecute state changes
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    // Determines if the command can execute
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    // Executes the command
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    // Raises the CanExecuteChanged event
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
