namespace EasySave.WPF.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

// Base class for all ViewModels implementing INotifyPropertyChanged
// This allows the UI to be notified when properties change
public abstract class BaseViewModel : INotifyPropertyChanged
{
    // Event raised when a property value changes
    public event PropertyChangedEventHandler? PropertyChanged;

    // Raises the PropertyChanged event for a property
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Sets a property value and raises PropertyChanged if the value changed
    // Returns true if the value was changed, false otherwise
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
