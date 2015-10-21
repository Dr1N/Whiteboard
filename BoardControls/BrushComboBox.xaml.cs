using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    /// Комбобокс для выбора цвета
    /// </summary>
	public partial class BrushComboBox : UserControl
	{
		public static readonly DependencyProperty SelectedIndexProperty;
		public static readonly DependencyProperty SelectedItemProperty;
        public static readonly RoutedEvent ColorChangedEvent ;
        public static readonly DependencyProperty IsEmptyColorProperty;

        private static bool isLoadedColors;

		static BrushComboBox()
		{
			SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(BrushComboBox));
			SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(SolidColorBrush), typeof(BrushComboBox));
            ColorChangedEvent = EventManager.RegisterRoutedEvent("ColorChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BrushComboBox));
            IsEmptyColorProperty = DependencyProperty.Register("IsEmptyColor", typeof(bool), typeof(BrushComboBox));
		}

        public BrushComboBox()
        {
            InitializeComponent();

            Binding selectedIndexBinding = new Binding("SelectedIndex");
            selectedIndexBinding.Source = Content;
            selectedIndexBinding.Mode = BindingMode.TwoWay;

            SetBinding(BrushComboBox.SelectedIndexProperty, selectedIndexBinding);

            Binding selectedItemBinding = new Binding("SelectedItem");
            selectedItemBinding.Source = Content;
            selectedItemBinding.Mode = BindingMode.TwoWay;

            SetBinding(BrushComboBox.SelectedItemProperty, selectedItemBinding);

            this.cbColor.ItemsSource = new BrushesToList(IsEmptyColor).Brushes;
        }

		public int SelectedIndex
		{
			get { return (int)GetValue(SelectedIndexProperty);	}
			set { SetValue(SelectedIndexProperty, value);		}
		}

		public object SelectedItem
		{
			get 
			{
				return GetValue(SelectedItemProperty) as SolidColorBrush;
			}
			set { SetValue(SelectedItemProperty, value); }
		}

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler(ColorChangedEvent, value); }
            remove { RemoveHandler(ColorChangedEvent, value); }
        }

        public bool IsEmptyColor
        {
            get { return (bool)GetValue(IsEmptyColorProperty); }
            set { SetValue(IsEmptyColorProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (IsEmptyColor && !isLoadedColors)
            {
                isLoadedColors = true;
                cbColor.ItemsSource = new BrushesToList(this.IsEmptyColor).Brushes;
            }

            ToolTip = (SelectedIndex == 0 || SelectedIndex == -1) ? "Нет заливки" : ToolTip = SelectedItem.ToString();
        }

        private void RaiseColorChangedEvent()
        {
            if (this.SelectedItem == null) { return; }
            this.ToolTip = (((SolidColorBrush)this.SelectedItem).Opacity == 0) ? "Нет заливки" : SelectedItem.ToString();
            RoutedEventArgs newEventArgs = new RoutedEventArgs(BrushComboBox.ColorChangedEvent);
            RaiseEvent(newEventArgs);
        }

        private void cbColor_DropDownClosed(object sender, EventArgs e)
        {
            RaiseColorChangedEvent();
        }
	}

    /// <summary>
    /// Список кистей
    /// </summary>
    public class BrushesToList
    {
        public IEnumerable<SolidColorBrush> Brushes { get; private set; }

        public BrushesToList(bool empty)
        {
            List<SolidColorBrush> brushes = new List<SolidColorBrush>();
            if (empty)
            {
                brushes.Add(new SolidColorBrush() { Opacity = 0, Color = Colors.White });
            }
            foreach (PropertyInfo propInfo in typeof(System.Windows.Media.Brushes).GetProperties(BindingFlags.Public | BindingFlags.Static))
                if (propInfo.PropertyType == typeof(SolidColorBrush))
                {
                    brushes.Add((SolidColorBrush)propInfo.GetValue(null, null));
                }

            Brushes = brushes;
        }
    }

    public class StrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush brush = value as SolidColorBrush;
            if (brush == null || brush.Opacity != 0) { return null; }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}