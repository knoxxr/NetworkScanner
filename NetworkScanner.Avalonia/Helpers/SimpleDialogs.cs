using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NetworkScanner.Avalonia.Helpers
{
    // Avalonia에는 WPF의 MessageBox 같은 내장 대화상자가 없어, 필요한 최소 기능만 담은 헬퍼.
    public static class SimpleDialogs
    {
        public static async Task ShowMessageAsync(Window owner, string message, string title = "알림")
        {
            var dialog = BuildDialog(title, message, out var buttonPanel);
            var ok = new Button { Content = "확인", Width = 80 };
            ok.Click += (_, _) => dialog.Close();
            buttonPanel.Children.Add(ok);

            await dialog.ShowDialog(owner);
        }

        public static async Task<bool> ShowConfirmAsync(Window owner, string message, string title = "확인")
        {
            var dialog = BuildDialog(title, message, out var buttonPanel);
            bool result = false;

            var yes = new Button { Content = "예", Width = 80 };
            yes.Click += (_, _) => { result = true; dialog.Close(); };
            var no = new Button { Content = "아니오", Width = 80 };
            no.Click += (_, _) => { result = false; dialog.Close(); };
            buttonPanel.Children.Add(yes);
            buttonPanel.Children.Add(no);

            await dialog.ShowDialog(owner);
            return result;
        }

        private static Window BuildDialog(string title, string message, out StackPanel buttonPanel)
        {
            buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
            };

            var layout = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 16 };
            layout.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            layout.Children.Add(buttonPanel);

            return new Window
            {
                Title = title,
                Width = 360,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = layout,
            };
        }
    }
}
