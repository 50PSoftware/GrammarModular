using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Grammar.Czech.Analyzer.Gui.ViewModels;

namespace Grammar.Czech.Analyzer.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // File picking stays in the view on purpose: it is a platform dialog, not application state, and
    // the view model would need an IStorageProvider abstraction just to be told a path it could equally
    // well receive straight from here.
    private async void OnBrowseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var provider = GetTopLevel(this)?.StorageProvider;

        if (provider is null)
        {
            return;
        }

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vyber textový soubor",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Textové soubory") { Patterns = ["*.txt", "*.docx", "*.odt"] },
                FilePickerFileTypes.All,
            ],
        });

        if (files.FirstOrDefault() is { } file)
        {
            viewModel.FilePath = file.Path.LocalPath;
        }
    }
}
