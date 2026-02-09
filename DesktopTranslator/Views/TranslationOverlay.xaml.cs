using System.Windows;
using System.Windows.Input;

namespace DesktopTranslator.Views;

public partial class TranslationOverlay : Window
{
    private bool _isPinned;

    public TranslationOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Show a loading state while translation is in progress.
    /// </summary>
    public void ShowLoading()
    {
        Dispatcher.Invoke(() =>
        {
            PlaceholderText.Visibility = Visibility.Collapsed;
            ResultScroller.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Visible;
            StatusText.Text = "翻译中...";
        });
    }

    /// <summary>
    /// Display the translation result text.
    /// </summary>
    public void ShowResult(string text)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Collapsed;
            ResultScroller.Visibility = Visibility.Visible;
            TranslationText.Text = text;
            CharCount.Text = $"{text.Length} 字符";
            StatusText.Text = $"翻译完成 · {DateTime.Now:HH:mm:ss}";
        });
    }

    /// <summary>
    /// Append streaming text chunk to the result.
    /// </summary>
    public void AppendStreamText(string chunk)
    {
        Dispatcher.Invoke(() =>
        {
            if (LoadingPanel.Visibility == Visibility.Visible)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                PlaceholderText.Visibility = Visibility.Collapsed;
                ResultScroller.Visibility = Visibility.Visible;
                TranslationText.Text = "";
            }
            TranslationText.Text += chunk;
            CharCount.Text = $"{TranslationText.Text.Length} 字符";
            StatusText.Text = "翻译中...";
        });
    }

    /// <summary>
    /// Mark streaming as complete.
    /// </summary>
    public void FinishStream()
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"翻译完成 · {DateTime.Now:HH:mm:ss}";
        });
    }

    /// <summary>
    /// Show an error message.
    /// </summary>
    public void ShowError(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Collapsed;
            ResultScroller.Visibility = Visibility.Visible;
            TranslationText.Text = $"翻译出错：{message}";
            TranslationText.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
            StatusText.Text = "出错";
        });
    }

    /// <summary>
    /// Reset error state for next translation.
    /// </summary>
    public void ResetErrorState()
    {
        Dispatcher.Invoke(() =>
        {
            TranslationText.Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
        });
    }

    /// <summary>
    /// Save the current window position/size to settings.
    /// </summary>
    public (double Left, double Top, double Width, double Height) GetWindowState()
    {
        return (Left, Top, Width, Height);
    }

    public void RestoreWindowState(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        PinIcon.Text = _isPinned ? "\uE841" : "\uE718";
        BtnPin.ToolTip = _isPinned ? "取消固定" : "固定窗口";
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TranslationText.Text))
        {
            Clipboard.SetText(TranslationText.Text);
            StatusText.Text = "已复制到剪贴板";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
