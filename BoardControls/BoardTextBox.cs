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
    public class BoardTextBox : TextBox
    {
        public int TabSize { get; set; }
        public double LineHeight 
        {
            get
            {
                Size sz = MeasureString("X");
                return sz.Height;
            }
            private set { } 
        }

        private int _maximumLines;
        private string _texBeforeChanging;
        private int _caretPosition;

        public BoardTextBox()
        {
            this.AcceptsReturn = true;
            this.AcceptsTab = true;
            this.TextWrapping = TextWrapping.NoWrap;
            this.SnapsToDevicePixels = true;
            this.IsUndoEnabled = false;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            Size sz = MeasureString("X");
            this._maximumLines = (int)(this.ActualHeight / sz.Height);
            this.LineHeight = sz.Height;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            this._texBeforeChanging = this.Text;
            this._caretPosition = this.CaretIndex;

            if (e.Key == Key.Tab && this.TabSize != 0)
            {
                string tab = new string(' ', this.TabSize);
                int caretPosition = this.CaretIndex;
                this.Text = this.Text.Insert(caretPosition, tab);
                this.CaretIndex = caretPosition + TabSize;
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (this.LineCount > this._maximumLines)
            {
                SystemSounds.Beep.Play();
                this.Text = this._texBeforeChanging;
                this.CaretIndex = this._caretPosition;
            }
            base.OnTextChanged(e);
        }

        private Size MeasureString(string text)
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
                this.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}