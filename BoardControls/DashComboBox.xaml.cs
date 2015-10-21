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

namespace BoardControls
{
    /// <summary>
    /// Комбобокс выбора типа линии
    /// </summary>
    public partial class DashComboBox : ComboBox
    {
        private DoubleCollection[] dashes = 
        {
            new DoubleCollection() {0.0},
            new DoubleCollection() {2.0},
            new DoubleCollection() {4.0},
            new DoubleCollection() {8.0},
            new DoubleCollection() {12.0},
        };

        public DashComboBox()
        {
            InitializeComponent();
            this.ItemsSource = this.dashes;
            this.SelectedIndex = 0;
        }
    }

    class DashArrayConerter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DoubleCollection dc = value as DoubleCollection;
            if (dc == null) { return null; }
            if (dc[0] == 0.0 ) 
            { 
                return null; 
            }
            else
                return dc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}