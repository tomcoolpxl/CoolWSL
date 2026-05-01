using CoolWSL.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoolWSL.App.Dialogs.ConfirmationDialogs;

public sealed class OperationConfirmationDialog : ContentDialog
{
    public OperationConfirmationDialog(OperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Title = request.Title;
        PrimaryButtonText = request.ConfirmButtonText;
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Close;
        Content = BuildContent(request);
    }

    private static UIElement BuildContent(OperationRequest request)
    {
        var panel = new StackPanel { Spacing = 12 };

        panel.Children.Add(new TextBlock
        {
            Text = $"Target: {request.TargetText}",
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Impact: {request.ImpactText}",
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        if (request.HasDetailText)
        {
            panel.Children.Add(new TextBlock
            {
                Text = request.DetailText,
                Opacity = 0.72,
                TextWrapping = TextWrapping.WrapWholeWords,
            });
        }

        return panel;
    }
}