using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using EasySave.WPF.ViewModels;

namespace EasySave.WPF.Views;

/// <summary>
/// Interaction logic for JobsView.xaml.
/// Code-behind is used ONLY for the folder-picker buttons (allowed per design constraints).
/// All other logic lives in JobsViewModel.
/// </summary>
public partial class JobsView : UserControl
{
    public JobsView()
    {
        InitializeComponent();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select source folder" };
        if (dlg.ShowDialog() == true && DataContext is JobsViewModel vm)
            vm.DialogSourcePath = dlg.FolderName;
    }

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select target folder" };
        if (dlg.ShowDialog() == true && DataContext is JobsViewModel vm)
            vm.DialogTargetPath = dlg.FolderName;
    }
}
