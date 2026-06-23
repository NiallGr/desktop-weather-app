using Avalonia.Controls;
using DesktopWeatherApp.ViewModels;

namespace DesktopWeatherApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                await vm.InitializeAsync();
        };
    }
}
