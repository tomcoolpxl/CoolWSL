using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CoolWSL.App.ViewModels;

namespace CoolWSL.App.Views.Controls;

public sealed partial class WslConfigKeyCard : UserControl
{
    public WslConfigKeyCard()
    {
        InitializeComponent();
    }

    public DistroSettingsRowViewModel ViewModel
    {
        get => (DistroSettingsRowViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DistroSettingsRowViewModel), typeof(WslConfigKeyCard), new PropertyMetadata(null, OnViewModelChanged));

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WslConfigKeyCard card)
        {
            card.Bindings.Update();
        }
    }
}
