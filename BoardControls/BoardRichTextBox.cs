using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace BoardControls
{
    public class BoardRichTextBox : RichTextBox
    {
        #region ПОЛЯ
        
        private int _lineCapacity;
        private int _caretOffsetBeforeChange;
        private FlowDocument _docBeforeChange;

        #endregion

        #region СВОЙСТВА
        
        public int Lines
        {
            get
            {
                return this.GetLines();
            }
        }
        public string Text
        {
            get
            {
                return new TextRange(this.Document.ContentStart, this.Document.ContentEnd).Text;
            }
            set
            {
                TextRange textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
                textRange.Text = value;
            }
        }
        public int CaretIndex
        {
            get
            {
                return this.CaretPosition.DocumentStart.GetOffsetToPosition(this.CaretPosition);
            }
            set
            {
                try
                {
                    this.CaretPosition.DocumentStart.GetPositionAtOffset(value, LogicalDirection.Forward);
                }
                catch { }
            }
        }
        public double LineHeight { get; private set; }

        #endregion

        public BoardRichTextBox()
        {
            //Зазор между параграфами

            Style style = new Style(typeof(Paragraph));
            style.Setters.Add(new Setter() { Property = Paragraph.MarginProperty, Value = new Thickness(0) });
            this.Resources.Add(typeof(Paragraph), style);

            //Отмена переноса текста(аля NoWrap)

            this.Document.PageWidth = 1000000;

            this.AcceptsTab = true;
            this.IsUndoEnabled = false;
            this.ContextMenu = null;
        }

        #region СОБЫТЯ

        //Расчёт максимального числа строк

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Size sz = MeasureString("Hello World");
            this._lineCapacity = (int)(this.ActualHeight / sz.Height);
            this.LineHeight = sz.Height;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            //Сохраняем состояние

            this._docBeforeChange = this.XamlClone<FlowDocument>(this.Document);
            this._caretOffsetBeforeChange = this.CaretPosition.DocumentStart.GetOffsetToPosition(this.CaretPosition);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (this._lineCapacity == 0) { return; }
            if (this.Lines > this._lineCapacity)
            {
                this.Document = this._docBeforeChange;
                try
                {
                    this.CaretPosition = this.CaretPosition.DocumentStart.GetPositionAtOffset(this._caretOffsetBeforeChange, LogicalDirection.Forward);
                }
                catch { }
                SystemSounds.Beep.Play();
            }
        }

        #endregion

        #region ПОМОШНИКИ

        /// <summary>
        /// Получить количество строк в поле
        /// </summary>
        /// <returns>Количество строк</returns>
        private int GetLines()
        {
            string text = this.Text;
            int count = 0;
            int position = 0;
            while ((position = text.IndexOf("\n", position)) != -1)
            {
                count++;
                position++;
            }
            return count;
        }

        /// <summary>
        /// Возвращает размер занимаемым текстом в поле, согласно настроек поля
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private Size MeasureString(string str)
        {
            var formattedText = new FormattedText(
                str,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
                this.FontSize,
                this.Foreground);
            return new Size(formattedText.Width, formattedText.Height);
        }

        /// <summary>
        /// Клонирование элемента интерфейса
        /// </summary>
        /// <typeparam name="T">Тип элемента</typeparam>
        /// <param name="source">Источник</param>
        /// <returns>Клон</returns>
        private T XamlClone<T>(T source)
        {
            string savedObject = System.Windows.Markup.XamlWriter.Save(source);

            StringReader stringReader = new StringReader(savedObject);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            T target = (T)System.Windows.Markup.XamlReader.Load(xmlReader);

            return target;
        }

        #endregion
    }
}