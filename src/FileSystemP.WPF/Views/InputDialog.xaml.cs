using System.Windows;

namespace FileSystemP.WPF.Views;

public partial class InputDialog : Window
{
    private InputDialog(string prompt, string? defaultValue)
    {
        InitializeComponent();
        PromptLabel.Content = prompt;
        InputBox.Text = defaultValue ?? string.Empty;
        InputBox.SelectAll();
        InputBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    public static string? Show(string prompt, string? defaultValue = null)
    {
        var dialog = new InputDialog(prompt, defaultValue);
        return dialog.ShowDialog() == true ? dialog.InputBox.Text : null;
    }
}
