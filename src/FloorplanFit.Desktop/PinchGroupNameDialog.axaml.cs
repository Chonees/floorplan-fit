using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FloorplanFit.Desktop;

public partial class PinchGroupNameDialog : Window
{
    public PinchGroupNameDialog()
    {
        InitializeComponent();
    }

    public PinchGroupNameDialog(string title, string description, string initialName)
        : this()
    {
        Title = title;
        TitleText.Text = title;
        DescriptionText.Text = description;
        NameTextBox.Text = initialName;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var groupName = NameTextBox.Text?.Trim() ?? string.Empty;
        if (groupName.Length == 0)
        {
            ValidationText.IsVisible = true;
            return;
        }

        Close(groupName);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
