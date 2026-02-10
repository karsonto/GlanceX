using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DesktopTranslator.Views;

public partial class MiniWidget : Window
{
    private bool _isDragging;
    private Point _dragStart;
    private Storyboard? _breathingStoryboard;
    private bool _isBreathing;

    /// <summary>
    /// Raised when the user clicks (not drags) the widget to restore the main window.
    /// </summary>
    public event Action? RestoreRequested;

    public MiniWidget()
    {
        InitializeComponent();
        PositionToBottomRight();
        BuildBreathingAnimation();
    }

    /// <summary>
    /// Position the widget at the bottom-right corner of the working area.
    /// </summary>
    private void PositionToBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
    }

    /// <summary>
    /// Build the breathing (pulse) storyboard for the brand dot.
    /// </summary>
    private void BuildBreathingAnimation()
    {
        var dotAnimation = new ColorAnimation
        {
            From = Color.FromRgb(0, 120, 212),       // #0078D4
            To = Color.FromArgb(100, 0, 120, 212),    // faded
            Duration = TimeSpan.FromSeconds(1.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        var glowAnimation = new DoubleAnimation
        {
            From = 0.0,
            To = 0.5,
            Duration = TimeSpan.FromSeconds(1.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        _breathingStoryboard = new Storyboard();

        Storyboard.SetTarget(dotAnimation, BrandDot);
        Storyboard.SetTargetProperty(dotAnimation,
            new PropertyPath("Fill.Color"));
        _breathingStoryboard.Children.Add(dotAnimation);

        Storyboard.SetTarget(glowAnimation, DotGlow);
        Storyboard.SetTargetProperty(glowAnimation,
            new PropertyPath(OpacityProperty));
        _breathingStoryboard.Children.Add(glowAnimation);
    }

    /// <summary>
    /// Start the breathing animation (call when translation is running).
    /// </summary>
    public void StartBreathing()
    {
        if (_isBreathing) return;
        _isBreathing = true;
        _breathingStoryboard?.Begin(this, true);
    }

    /// <summary>
    /// Stop the breathing animation (call when translation stops).
    /// </summary>
    public void StopBreathing()
    {
        if (!_isBreathing) return;
        _isBreathing = false;
        _breathingStoryboard?.Stop(this);

        // Reset to static brand color
        DotBrush.Color = Color.FromRgb(0, 120, 212);
        DotGlow.Opacity = 0;
    }

    // ==================== Drag & Click Handling ====================

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        _dragStart = e.GetPosition(this);
        WidgetBorder.CaptureMouse();
    }

    private void Widget_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !WidgetBorder.IsMouseCaptured)
            return;

        var current = e.GetPosition(this);
        var delta = current - _dragStart;

        // Start dragging after a small threshold to distinguish from click
        if (!_isDragging && (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            Left += delta.X;
            Top += delta.Y;
        }
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        WidgetBorder.ReleaseMouseCapture();

        if (!_isDragging)
        {
            // It was a click, not a drag → restore main window
            RestoreRequested?.Invoke();
        }

        _isDragging = false;
    }

    // ==================== Hover Effects ====================

    private void Widget_MouseEnter(object sender, MouseEventArgs e)
    {
        var scaleUp = new DoubleAnimation(1.05, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        WidgetScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
        WidgetScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

        WidgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    }

    private void Widget_MouseLeave(object sender, MouseEventArgs e)
    {
        var scaleDown = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        WidgetScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
        WidgetScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);

        WidgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232));
    }
}
