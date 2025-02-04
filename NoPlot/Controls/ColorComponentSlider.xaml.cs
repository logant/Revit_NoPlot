using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LINECommon.Windows.Controls
{
    /// <summary>
    /// Interaction logic for ColorComponentSlider.xaml
    /// </summary>
    public partial class ColorComponentSlider : UserControl
    {
        private Color _color;
        private Color _darkened;
        private string _prefix;
        private Color _textColor = Colors.WhiteSmoke;
        
        public SolidColorBrush MainColor => new SolidColorBrush(_color);
        public SolidColorBrush Darkened => new SolidColorBrush(_darkened);
        public SolidColorBrush TextForeground => new SolidColorBrush(_textColor);

        public event EventHandler SliderValueChanged;

        public event EventHandler SliderDoubleClicked;

        public event EventHandler TextInputComplete;
        
        public double Value
        {
            get { return ComponentSlider.Value; }
            set { ComponentSlider.Value = value; }
        }

        public string Text => $"{_prefix}: {Convert.ToInt32(Value * 255):000}";

        public string Prefix => _prefix;
        
        public ColorComponentSlider()
        {
            InitializeComponent();
            _color = Colors.Blue;
            _prefix = "B";
            GetDarkened();
            SwatchRect.Width = 41;
        }

        public ColorComponentSlider(Color color, string prefix)
        {
            _color = color;
            _prefix = prefix;
            GetDarkened();

            InitializeComponent();
            SwatchRect.Width = 41;
        }

        public void SetColor(Color color, string prefix, int startValue)
        {
            try
            {
                _color = color;
                _prefix = prefix;
                GetDarkened();
                if (_prefix == "α")
                    _textColor = Color.FromRgb(30, 30, 30);
                //ValueTextBlock.Text = $"{_prefix}: {Convert.ToInt32(ComponentSlider.Value * 255):000}";
                ComponentSlider.Value = startValue / 255.0;
                // ColorRGBSelector is set to 290 pixels wide with 10 pixel buffer on either side of
                // ColorComponentSlider element. Then the actual slider control on this element is 
                // offset 40 pixels on the left. So 290 - (10 + 10 + 40) should give us a width of 230;
                var sliderWidth = 250;

                SwatchRect.Width = ((sliderWidth - 40) * ComponentSlider.Value) + 21;
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        private void GetDarkened()
        {
            float correctionFactor = 0.8f;
            float red = (float)_color.R * correctionFactor;
            float green = (float)_color.G * correctionFactor;
            float blue = (float)_color.B * correctionFactor;
            _darkened = Color.FromRgb((byte)red, (byte)green, (byte)blue);
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Adjust the size of the rectangle
            Slider s = sender as Slider;
            var width = s.ActualWidth;
            var sw = ((width - 40) * s.Value) + 21;
            if (sw > 0)
                SwatchRect.Width = sw;

            //ValueTextBlock.Text = $"{_prefix}: {Convert.ToInt32(s.Value * 255):000}";
            if(SliderValueChanged != null)
                SliderValueChanged(this, e);
        }

        private void ComponentSlider_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // hide the slider and border
            ComponentSlider.Visibility = Visibility.Hidden;
            SwatchRect.Visibility = Visibility.Hidden;
            // show the textbox (TBD)
            ValueTextBox.Visibility = Visibility.Visible;
            ValueTextBox.Text = $"{Math.Round(ComponentSlider.Value * 255.0):###}";
            ValueTextBox.Focus();
            ValueTextBox.SelectAll();

            if (SliderDoubleClicked != null)
                SliderDoubleClicked(this, e);
        }



        private void ValueTextBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tb = sender as TextBox;
                if (int.TryParse(tb.Text, out int val))
                {
                    var pct = (double)val / 255.0;
                    pct = val <= 255 ? (double)val / 255.0 : 1.0;
                    if (pct < 0)
                        pct = 0.0;
                    ComponentSlider.Value = pct;
                }

                ValueTextBox.Visibility = Visibility.Hidden;
                SwatchRect.Visibility = Visibility.Visible;
                ComponentSlider.Visibility = Visibility.Visible;
                
                if (TextInputComplete != null)
                    TextInputComplete(this, e);
                
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                // cancel and do nothing.
                ValueTextBox.Visibility = Visibility.Hidden;
                SwatchRect.Visibility = Visibility.Visible;
                ComponentSlider.Visibility = Visibility.Visible;

                if (TextInputComplete != null)
                    TextInputComplete(this, e);

                e.Handled = true;

            }
        }
    }
}
