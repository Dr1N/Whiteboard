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
    /// Комбобокс для выбора толщины линии
    /// </summary>
    public partial class ThicknessComboBox : ComboBox
    {
        private double[] thickness = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 };
        public ThicknessComboBox()
        {
            InitializeComponent();
            this.ItemsSource = this.thickness;
            this.SelectedIndex = 0;
        }
    }
}