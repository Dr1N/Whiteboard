using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xaml;

namespace BoardControls
{
    /// <summary>
    /// Текстовое поле для добавления/редактрования надписи
    /// </summary>
    public class LabelTextBox : TextBox
    {
        public LabelTextBox()
        {
            this.AcceptsReturn = true;
            this.FontWeight = FontWeights.Bold;
            this.BorderThickness = new Thickness(0);

            Setter borderSetter = new Setter();
            borderSetter.Property = TextBox.BorderThicknessProperty;
            borderSetter.Value = new Thickness(0);

            Trigger readOnlyTrigger = new Trigger();
            readOnlyTrigger.Property = TextBox.IsReadOnlyProperty;
            readOnlyTrigger.Value = true;
            readOnlyTrigger.Setters.Add(borderSetter);

            Setter borderFocusSetter = new Setter();
            borderFocusSetter.Property = TextBox.BorderThicknessProperty;
            borderFocusSetter.Value = new Thickness(0);

            Trigger focusTrigger = new Trigger();
            focusTrigger.Property = TextBox.IsFocusedProperty;
            focusTrigger.Value = true;
            focusTrigger.Setters.Add(borderFocusSetter);

            Style style = new Style();
            style.Triggers.Add(readOnlyTrigger);
            style.Triggers.Add(focusTrigger);

            this.Style = style;
            this.SnapsToDevicePixels = true;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            IsReadOnly = true;
        }

        protected override void OnMouseDoubleClick(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                IsReadOnly = false;
            }
        }
    }
}