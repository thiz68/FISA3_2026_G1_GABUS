using System.Windows;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

/// <summary>
/// Interaction logic for BackupProgressWindow.xaml
/// Window displaying real-time backup progress for all jobs
/// </summary>
public partial class BackupProgressWindow : Window
{
    public BackupProgressWindow(BackupProgressViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Set the close action so the ViewModel can close the window
        viewModel.CloseAction = () => this.Close();
    }
}
