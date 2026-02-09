using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopTranslator.Helpers;

namespace DesktopTranslator.Views;

public partial class RegionSelector : Window
{
    private Point _startPoint;
    private bool _isDragging;
    private double _dpiScale = 1.0;

    /// <summary>
    /// The selected region in physical (device) pixels.
    /// Null if the user cancelled.
    /// </summary>
    public System.Drawing.Rectangle? SelectedRegion { get; private set; }

    public RegionSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Get DPI scale for this window
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScale = source.CompositionTarget.TransformToDevice.M11;
        }

        // Ensure Normal state before setting manual size
        WindowState = WindowState.Normal;

        // Cover all monitors (virtual screen = union of all displays)
        var virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        Left = virtualLeft / _dpiScale;
        Top = virtualTop / _dpiScale;
        Width = virtualWidth / _dpiScale;
        Height = virtualHeight / _dpiScale;

        Activate();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(OverlayCanvas);
        _isDragging = true;

        SelectionBorder.Visibility = Visibility.Visible;
        SizeIndicator.Visibility = Visibility.Visible;

        Canvas.SetLeft(SelectionBorder, _startPoint.X);
        Canvas.SetTop(SelectionBorder, _startPoint.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;

        OverlayCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPoint = e.GetPosition(OverlayCanvas);

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var w = Math.Abs(currentPoint.X - _startPoint.X);
        var h = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;

        // Update size indicator
        var physW = (int)(w * _dpiScale);
        var physH = (int)(h * _dpiScale);
        SizeText.Text = $"{physW} × {physH}";
        Canvas.SetLeft(SizeIndicator, x);
        Canvas.SetTop(SizeIndicator, y + h + 8);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        OverlayCanvas.ReleaseMouseCapture();

        var currentPoint = e.GetPosition(OverlayCanvas);
        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var w = Math.Abs(currentPoint.X - _startPoint.X);
        var h = Math.Abs(currentPoint.Y - _startPoint.Y);

        // Minimum size check
        if (w < 10 || h < 10)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
            return;
        }

        // Convert WPF logical pixels to physical device pixels
        // Account for virtual screen offset
        var virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);

        SelectedRegion = new System.Drawing.Rectangle(
            (int)(x * _dpiScale) + virtualLeft,
            (int)(y * _dpiScale) + virtualTop,
            (int)(w * _dpiScale),
            (int)(h * _dpiScale));

        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
        }
    }
}
