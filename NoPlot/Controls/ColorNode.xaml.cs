using System;
using System.Collections.Generic;
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
    /// Interaction logic for ColorNode.xaml
    /// </summary>
    public partial class ColorNode : UserControl
    {
        Color _color = Colors.Black;
        public Color Color => _color;
        public ColorNode() : this(Color.FromArgb(255,0,0,0))
        {
            
        }

        public ColorNode(Color color)
        {
            _color = color;
            InitializeComponent();
            InnerEllipse.Fill = new SolidColorBrush(Color);
            
        }

        public void ChangeColor(Color color)
        {
            _color = color;
            InnerEllipse.Fill = new SolidColorBrush(Color);
        }
    }
}
