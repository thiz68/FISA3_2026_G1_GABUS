using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

public partial class ActivityView : UserControl
{
    public ActivityView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ActivityViewModel old)
            old.ScrollToBottomRequested -= OnScrollToBottomRequested;

        if (e.NewValue is ActivityViewModel vm)
            vm.ScrollToBottomRequested += OnScrollToBottomRequested;
    }

    private void OnScrollToBottomRequested(object? sender, EventArgs e)
    {
        // Schedule after layout so the ListBox has rendered the new items
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (LogList.Items.Count > 0)
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        }));
    }
}
