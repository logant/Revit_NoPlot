using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LINECommon.Windows.Controls
{
    /// <summary>
    /// Interaction logic for ColorRGBSelector.xaml
    /// </summary>
    public partial class ColorRGBSelector : UserControl
    {
        private int _red = 0;
        private int _green = 0;
        private int _blue = 128;
        private int _alpha = 255;

        private int _nodeX = 0;
        private int _nodeY = 0;
        private bool _allowMove = false;
        private Point _mouseStart = new Point(0, 0);
        private Point _controlStart = new Point(0, 0);

        public double Red => _red;
        public double Green => _green;
        public double Blue => _blue;

        public Color TopLeftColor { get; set; }
        public Color TopRight { get; set; }
        public Color BottomRight { get; set; }
        public Color BottomLeft { get; set; }

        public LinearGradientBrush BaseBrush { get; set; }
        public RadialGradientBrush MiddleBrush { get; set; }
        public RadialGradientBrush TopBrush { get; set; }

        public Color SelectedColor => Color.FromArgb((byte)_alpha, (byte)_red, (byte)_green, (byte)_blue);

        public ColorRGBSelector(Color initColor)
        {
            DataContext = this;
            InitializeComponent();
            SetColor(initColor);

        }

        public ColorRGBSelector() : this(Color.FromArgb(255, 0, 0, 0))
        {
        }

        public void SetColor(Color color)
        {
            _alpha = color.A;
            _red = color.R;
            _green = color.G;
            _blue = color.B;

            TopLeftColor = Color.FromArgb((byte)_alpha,0, 255, (byte)_blue);
            TopRight = Color.FromArgb((byte)_alpha, 255, 255, (byte)_blue);
            BottomRight = Color.FromArgb((byte)_alpha, 255, 0, (byte)_blue);
            BottomLeft = Color.FromArgb((byte)_alpha, 0, 0, (byte)_blue);

            BaseBrush = new LinearGradientBrush(TopLeftColor, TopRight, new Point(0, 0), new Point(1, 0));
            MiddleBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(1, 1),
                Center = new Point(1, 1),
                RadiusX = 1.41,
                RadiusY = 1.41
            };
            MiddleBrush.GradientStops.Add(new GradientStop(BottomRight, 0.05));
            MiddleBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.8));

            TopBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0, 1),
                Center = new Point(0, 1),
                RadiusX = 1.4,
                RadiusY = 1.41
            };
            TopBrush.GradientStops.Add(new GradientStop(BottomLeft, 0.05));
            TopBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.9));

            BlueSlider.Value = (double)_blue / 255.0;

            if (CNode != null)
            {
                int cornerOffset = Convert.ToInt32(Math.Round(BlueSlider.Value * 100 * Math.Sin(Math.PI * 0.25)));

                RedGreenGrid.Margin = new Thickness(cornerOffset + _nodeX, 271 - _nodeY - cornerOffset, 0, 0);
                CNode.ChangeColor(Color.FromRgb((byte)_red, (byte)_green, (byte)_blue));
                // change CNode position
                var top = -((_green / 255.0) * 200);
                var right = _red / 255.0 * 200;
                CNode.Margin = new Thickness(right, top, 0, 0);
            }

            if (RedCompSlider != null)
            {
                RedCompSlider.SetColor(Color.FromRgb(180, 0, 0), "R", _red);
                GreenCompSlider.SetColor(Color.FromRgb(0, 180, 0), "G", _green);
                BlueCompSlider.SetColor(Color.FromRgb(0, 0, 180), "B", _blue);
                // TODO: Figure out what's wrong with the alfa slider
                AlphaCompSlider.SetColor(Color.FromRgb(200, 200, 200), "α", _alpha);
            }
        }

        private void UpdateColorPlane()
        {
            TopLeftColor = Color.FromArgb((byte)_alpha, 0, 255, (byte)_blue);
            TopRight = Color.FromArgb((byte)_alpha, 255, 255, (byte)_blue);
            BottomRight = Color.FromArgb((byte)_alpha, 255, 0, (byte)_blue);
            BottomLeft = Color.FromArgb((byte)_alpha, 0, 0, (byte)_blue);

            if (BaseBrush is null)
                return;

            BaseBrush.GradientStops[0].Color = TopLeftColor;
            BaseBrush.GradientStops[1].Color = TopRight;
            MiddleBrush.GradientStops[0].Color = BottomRight;
            TopBrush.GradientStops[0].Color = BottomLeft;

            if (CNode != null)
            {
                int cornerOffset = Convert.ToInt32(Math.Round(BlueSlider.Value * 100 * Math.Sin(Math.PI * 0.25)));

                RedGreenGrid.Margin = new Thickness(cornerOffset + _nodeX, 271 - _nodeY - cornerOffset, 0, 0);
                CNode.ChangeColor(Color.FromRgb((byte)_red, (byte)_green, (byte)_blue));
            }
        }

        private void BlueSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = sender as Slider;

            _blue = Convert.ToInt32(Math.Round(s.Value * 255));
            UpdateColorPlane();

            // Adjust blue component slider
            if (BlueCompSlider != null)
                BlueCompSlider.Value = _blue / 255.0;
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _allowMove = true;
            _controlStart.X = CNode.Margin.Left;
            _controlStart.Y = CNode.Margin.Top;
            _mouseStart.X = e.GetPosition(this.Parent as Canvas).X;
            _mouseStart.Y = e.GetPosition(this.Parent as Canvas).Y;
            UIElement el = (UIElement)sender;
            el.CaptureMouse();
        }

        private void CNode_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_allowMove || e.LeftButton != MouseButtonState.Pressed)
                return;


            double x = e.GetPosition(this.Parent as Canvas).X;
            double y = e.GetPosition(this.Parent as Canvas).Y;
            double shiftX = x - _mouseStart.X;
            double shiftY = y - _mouseStart.Y;
            double left = _controlStart.X + shiftX;
            double top = _controlStart.Y + shiftY;
            if (left < 0)
                left = 0;
            if (left > 200)
                left = 200;
            if (top > 0)
                top = 0;
            if (top < -200)
                top = -200;
            CNode.Margin = new Thickness(left, top, 0, 0);
            double rpct = left / 200.0;
            double gpct = Math.Abs(top) / 200.0;
            _red = Convert.ToInt32(Math.Round(rpct * 255));
            _green = Convert.ToInt32(Math.Round(gpct * 255));
            CNode.ChangeColor(Color.FromArgb((byte)_alpha, (byte)_red, (byte)_green, (byte)_blue));

            // Adjust red/green component sliders
            RedCompSlider.Value = _red / 255.0;
            GreenCompSlider.Value = _green / 255.0;
        }

        private void CNode_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _allowMove = false;
            UIElement el = (UIElement)sender;
            el.ReleaseMouseCapture();
        }

        private void Slider_OnSliderValueChanged(object sender, EventArgs e)
        {
            var slider = sender as ColorComponentSlider;
            if (slider == null)
                return;

            switch (slider.Prefix)
            {
                case "R":
                    // Modify the _red value and adjust CNode;
                    _red = Convert.ToInt32(Math.Round(slider.Value * 255));
                    var left = _red / 255.0 * 200;
                    CNode.Margin = new Thickness(left, CNode.Margin.Top, 0, 0);
                    break;
                case "G":
                    _green = Convert.ToInt32(Math.Round(slider.Value * 255));
                    var top = -((_green / 255.0) * 200);
                    CNode.Margin = new Thickness(CNode.Margin.Left, top, 0, 0);
                    break;
                case "B":
                    _blue = Convert.ToInt32(Math.Round(slider.Value * 255));
                    BlueSlider.Value = slider.Value;
                    break;
                case "α":
                    _alpha = Convert.ToInt32(Math.Round(slider.Value * 255));
                    UpdateColorPlane();
                    break;
            }
            CNode.ChangeColor(Color.FromArgb((byte)_alpha, (byte)_red, (byte)_green, (byte)_blue));
        }

        private void Slider_OnSliderDoubleClicked(object sender, EventArgs e)
        {
            var slider = sender as ColorComponentSlider;
            if (slider == null)
                return;

            switch (slider.Prefix)
            {
                case "R":
                    GreenCompSlider.IsEnabled = false;
                    BlueCompSlider.IsEnabled = false;
                    BlueSlider.IsEnabled = false;
                    AlphaCompSlider.IsEnabled = false;
                    CNode.IsEnabled = false;
                    break;
                case "G":
                    RedCompSlider.IsEnabled = false;
                    BlueCompSlider.IsEnabled = false;
                    BlueSlider.IsEnabled = false;
                    AlphaCompSlider.IsEnabled = false;
                    CNode.IsEnabled = false;
                    break;
                case "B":
                    RedCompSlider.IsEnabled = false;
                    GreenCompSlider.IsEnabled = false;
                    BlueSlider.IsEnabled = false;
                    AlphaCompSlider.IsEnabled = false;
                    CNode.IsEnabled = false;
                    break;
                case "α":
                    RedCompSlider.IsEnabled = false;
                    GreenCompSlider.IsEnabled = false;
                    BlueSlider.IsEnabled = false;
                    BlueCompSlider.IsEnabled = false;
                    CNode.IsEnabled = false;
                    break;
            }
        }

        private void Slider_OnTextInputComplete(object sender, EventArgs e)
        {
            var slider = sender as ColorComponentSlider;
            if (slider == null)
                return;

            switch (slider.Prefix)
            {
                case "R":
                    GreenCompSlider.IsEnabled = true;
                    BlueCompSlider.IsEnabled = true;
                    BlueSlider.IsEnabled = true;
                    AlphaCompSlider.IsEnabled = true;
                    CNode.IsEnabled = true;
                    break;
                case "G":
                    RedCompSlider.IsEnabled = true;
                    BlueCompSlider.IsEnabled = true;
                    BlueSlider.IsEnabled = true;
                    AlphaCompSlider.IsEnabled = true;
                    CNode.IsEnabled = true;
                    break;
                case "B":
                    RedCompSlider.IsEnabled = true;
                    GreenCompSlider.IsEnabled = true;
                    BlueSlider.IsEnabled = true;
                    AlphaCompSlider.IsEnabled = true;
                    CNode.IsEnabled = true;
                    break;
                case "α":
                    RedCompSlider.IsEnabled = true;
                    GreenCompSlider.IsEnabled = true;
                    BlueSlider.IsEnabled = true;
                    BlueCompSlider.IsEnabled = true;
                    CNode.IsEnabled = true;
                    break;

            }
        }
    }
}
