using LegacySnapshot.ViewModels;

namespace LegacySnapshot.Views;

public partial class ProceduresPage : ContentPage
{
    private readonly ProceduresViewModel _viewModel;

    public ProceduresPage(ProceduresViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadProceduresCommand.Execute(null);
    }
}
