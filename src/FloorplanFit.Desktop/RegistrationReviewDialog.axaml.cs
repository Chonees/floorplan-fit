using Avalonia.Controls;
using Avalonia.Interactivity;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop;

public partial class RegistrationReviewDialog : Window
{
    public RegistrationReviewDialog()
    {
        InitializeComponent();
    }

    public RegistrationReviewDialog(RegistrationReviewData review)
        : this()
    {
        ArgumentNullException.ThrowIfNull(review);
        DataContext = review;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
        => Close(false);

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RegistrationReviewData { CanConfirm: true })
        {
            Close(true);
        }
    }
}
