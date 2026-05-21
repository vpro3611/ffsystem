using System.Windows;

namespace FileSystemP.WPF.Views;

public partial class ContentDialog : Window
{
    private ContentDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        NameBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    public static new (string name, string content)? Show()
    {
        var dialog = new ContentDialog();
        if (dialog.ShowDialog() != true) return null;
        return (dialog.NameBox.Text, dialog.ContentBox.Text);
    }
}
