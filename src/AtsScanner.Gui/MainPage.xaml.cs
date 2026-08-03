using AtsScanner.Gui.ViewModels;

namespace AtsScanner.Gui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        BindingContext = _viewModel;
    }

    private async void OnChooseFileClicked(object? sender, EventArgs e) =>
        await _viewModel.PickFileAsync();

    private void OnSelectAllClicked(object? sender, EventArgs e) =>
        _viewModel.SelectAllCommand.Execute(null);

    private void OnSelectNoneClicked(object? sender, EventArgs e) =>
        _viewModel.SelectNoneCommand.Execute(null);

    private void OnToggleExpandClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: ScanResultViewModel result })
            result.IsExpanded = !result.IsExpanded;
    }
}

