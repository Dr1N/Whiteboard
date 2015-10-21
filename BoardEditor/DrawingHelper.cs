using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using BoardControls;

namespace BoardEditor
{
    /// <summary>
    /// Класс для ввода информации на доску
    /// </summary>
    class DrawingHelper
    {
        #region ПОЛЯ РИСОВАНИЕ

        private readonly double _RECT_RADIUS = 15.0;
        private int _pointCnt;

        private Editor _editor;                //Ссылка на окно редактора
        public bool _isDrawing;                //Рисуется ли фигура
        public BOADR_DRAW_SHAPE _drawMode;     //Текущий режим редактирования
        private Point _beginPoint;             //Точка начала рисования
        private Shape _currentShape;           //Рисуемая фигура 

        #endregion

        #region ВСПОМОГАТЕЛЬНЫЕ ДАННЫЕ

        private Dictionary<InkCanvasEditingMode, string> _inkModesDictinary = new Dictionary<InkCanvasEditingMode, string>()
        {
            { InkCanvasEditingMode.None, "Инструмент: Ввод текста" },
            { InkCanvasEditingMode.EraseByPoint, "Инструмент: Ластик для заметок (удаляет только рукописные заметки)" },
            { InkCanvasEditingMode.EraseByStroke, "Инструмент: Удаление заметок (удаляет только рукописные заметки)" },
            { InkCanvasEditingMode.Ink, "Инструмент: Маркер" },
            { InkCanvasEditingMode.Select, "Инструмент: Выбор/редактирование элементов (для удаления выделете объект и нажмите Delete на клавиатуре)" }
        };

        private Dictionary<BOADR_DRAW_SHAPE, string> _shapeModesDictinary = new Dictionary<BOADR_DRAW_SHAPE, string>() 
        {
            { BOADR_DRAW_SHAPE.POLYLINE, "Инструмент: Карандаш" },
            { BOADR_DRAW_SHAPE.LINE, "Инструмент: Прямая" },
            { BOADR_DRAW_SHAPE.RECTANGLE, "Инструмент: Прямоугольник" },
            { BOADR_DRAW_SHAPE.ROUND_RECTANGLE, "Инструмент: Закруглённый прямоугольник" }, 
            { BOADR_DRAW_SHAPE.ELLIPSE, "Инструмент: Элипс" }, 
            { BOADR_DRAW_SHAPE.LABEL, "Инструмент: Надпись" } 
        };

        #endregion

        #region КОНСТРУКТОР
        
        public DrawingHelper(Editor editor)
        {
            this._editor = editor;
        }

        #endregion

        #region РИСОВАНИЕ
        
        /// <summary>
        /// Установить режим рисования согласно выбранному переключателю на панелях инструментов
        /// </summary>
        /// <param name="mode">Режим рисования (Enum - InkCanvasEditingMode или BOADR_DRAW_SHAPE)</param>
        public void SetShape(Object mode)
        {
            if (mode is InkCanvasEditingMode)                                               //Режимы InkCanvas 
            {
                this._editor.sbTool.Text = this._inkModesDictinary[(InkCanvasEditingMode)mode];
                this._editor.tbBoard.Focusable = true;
                this._editor.inkBoard.EditingMode = (InkCanvasEditingMode)mode;
                this._editor.tbFontSetting.IsEnabled = false;

                if (this._editor.inkBoard.EditingMode == InkCanvasEditingMode.None)        //Ввод текста
                {
                    this._editor.tbBoard.Focus();
                    this._editor.tbShapeSetting.IsEnabled = false;
                    this._editor.sbCoord.Text = "Координаты:";
                }
                else                                                                       //Рисование и редактироване
                {
                    this._editor.inkBoard.Focus();
                    this._editor.tbShapeSetting.IsEnabled = this._editor.inkBoard.EditingMode == InkCanvasEditingMode.Ink;
                    this._editor.inkBoard.DefaultDrawingAttributes.Color = ((SolidColorBrush)this._editor.cbStrokeColor.SelectedItem).Color;
                    this._editor.inkBoard.DefaultDrawingAttributes.Width = (double)this._editor.cbThickness.SelectedItem * 4;
                    this._editor.inkBoard.DefaultDrawingAttributes.Height = (double)this._editor.cbThickness.SelectedItem * 4;
                }
                this._drawMode = BOADR_DRAW_SHAPE.NONE;
            }
            else if (mode is BOADR_DRAW_SHAPE)                                              //Режимы рисования фигур
            {
                this._editor.tbBoard.Focusable = false;
                this._editor.inkBoard.EditingMode = InkCanvasEditingMode.None;
                this._editor.inkBoard.Focus();
                this._drawMode = (BOADR_DRAW_SHAPE)mode;
                this._editor.tbShapeSetting.IsEnabled = true;
                this._editor.tbFontSetting.IsEnabled = ((BOADR_DRAW_SHAPE)mode) == BOADR_DRAW_SHAPE.LABEL;
                this._editor.sbTool.Text = this._shapeModesDictinary[(BOADR_DRAW_SHAPE)mode];
            }
        }

        /// <summary>
        /// Добавить надпись на холст 
        /// Компонет основан на TextBox - поэтому тут своя атмосфера
        /// </summary>
        /// <param name="coord">Координаты надписи (правый левый угол)</param>
        public void AddLabel(Point coord)
        {
            LabelTextBox labelBox = new LabelTextBox();
            labelBox.Foreground = (Brush)this._editor.cbStrokeColor.SelectedItem;
            labelBox.Background = (Brush)this._editor.cbFillColor.SelectedItem;
            labelBox.ToolTip = "Двойной клик для редактирования";
            labelBox.FontFamily = (FontFamily)this._editor.cbFontFamaly.SelectedItem;
            labelBox.FontSize = Double.Parse(((this._editor.cbFontSize.SelectedItem as ComboBoxItem).Content.ToString()));
            InkCanvas.SetLeft(labelBox, coord.X);
            InkCanvas.SetTop(labelBox, coord.Y);
            this._editor.inkBoard.Children.Add(labelBox);
            labelBox.Focus();
            labelBox.LostFocus += (s, e) =>
            {
                this._editor.rbType.IsChecked = true;
                Mouse.OverrideCursor = null;
                if (labelBox.Text.Trim().Length == 0)
                {
                    this._editor.inkBoard.Children.Remove(labelBox);
                }
                else
                {
                    this._editor.SaveState();
                    this._editor.ResetClientsUpdate();
                }
            };
        }

        /// <summary>
        /// Начало рисования фигуры
        /// </summary>
        /// <param name="beginPoint">Точка начала фигуры</param>
        public void BeginDrawingShape(Point beginPoint)
        {
            if (this._editor.inkBoard.Children.Count >= 50)
            {
                MessageBox.Show("Достигнуто максиальное колчичество элементов", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Добавление надписи

            if (this._drawMode == BOADR_DRAW_SHAPE.LABEL)
            {
                this.AddLabel(beginPoint);
            }
            else
            {

                //Создание фигуры

                this._isDrawing = true;
                this._beginPoint = beginPoint;

                switch (this._drawMode)
                {
                    case BOADR_DRAW_SHAPE.POLYLINE:
                        this._currentShape = new Polyline();
                        ((Polyline)this._currentShape).StrokeLineJoin = PenLineJoin.Round;
                        ((Polyline)this._currentShape).Points.Add(beginPoint);
                        break;
                    case BOADR_DRAW_SHAPE.LINE:
                        this._currentShape = new Line();
                        ((Line)this._currentShape).X1 = this._beginPoint.X;
                        ((Line)this._currentShape).Y1 = this._beginPoint.Y;
                        ((Line)this._currentShape).X2 = this._beginPoint.X;
                        ((Line)this._currentShape).Y2 = this._beginPoint.Y;
                        break;
                    case BOADR_DRAW_SHAPE.RECTANGLE:
                        this._currentShape = new Rectangle();
                    SetPosition:
                        InkCanvas.SetLeft(this._currentShape, this._beginPoint.X);
                        InkCanvas.SetTop(this._currentShape, this._beginPoint.Y);
                        break;
                    case BOADR_DRAW_SHAPE.ROUND_RECTANGLE:
                        this._currentShape = new Rectangle();
                        ((Rectangle)this._currentShape).RadiusX = this._RECT_RADIUS;
                        ((Rectangle)this._currentShape).RadiusY = this._RECT_RADIUS;
                        goto SetPosition;
                    case BOADR_DRAW_SHAPE.ELLIPSE:
                        this._currentShape = new Ellipse();
                        goto SetPosition;
                    default:
                        break;
                }

                //Настройка фигуры согласно настроек панели инструментов

                this._currentShape.SnapsToDevicePixels = true;
                this._currentShape.Stroke = (Brush)this._editor.cbStrokeColor.SelectedItem;
                if (this._drawMode != BOADR_DRAW_SHAPE.POLYLINE)
                {
                    this._currentShape.Fill = (Brush)this._editor.cbFillColor.SelectedItem;
                }
                this._currentShape.StrokeThickness = (double)this._editor.cbThickness.SelectedItem;
                this._currentShape.StrokeDashArray = (((DoubleCollection)this._editor.cbDash.SelectedItem)[0] == 0.0) ? null : (DoubleCollection)this._editor.cbDash.SelectedItem;
                this._editor.inkBoard.Children.Add(this._currentShape);
            }
        }

        /// <summary>
        /// Скользящее рисование фигуры
        /// </summary>
        /// <param name="currentPoint">Текущая координата фигуры</param>
        public void DrawingShape(Point currentPoint)
        {
            switch (this._drawMode)
            {
                case BOADR_DRAW_SHAPE.POLYLINE:
                    this._pointCnt++;
                    if (this._pointCnt % 2 == 0)
                    {
                        ((Polyline)this._currentShape).Points.Add(currentPoint);
                        this._pointCnt = 0;
                    }
                    break;
                case BOADR_DRAW_SHAPE.LINE:
                    ((Line)this._currentShape).X2 = currentPoint.X;
                    ((Line)this._currentShape).Y2 = currentPoint.Y;
                    break;
                case BOADR_DRAW_SHAPE.RECTANGLE:
                case BOADR_DRAW_SHAPE.ROUND_RECTANGLE:
                case BOADR_DRAW_SHAPE.ELLIPSE:
                    double width = currentPoint.X - this._beginPoint.X;
                    double height = currentPoint.Y - this._beginPoint.Y;

                    //Нормализация фигуры

                    if (width < 0 && height < 0)
                    {
                        InkCanvas.SetLeft(this._currentShape, currentPoint.X);
                        InkCanvas.SetTop(this._currentShape, currentPoint.Y);
                    }
                    else if (width > 0 && height < 0)
                    {
                        InkCanvas.SetTop(this._currentShape, currentPoint.Y);
                    }
                    else if (width < 0 && height > 0)
                    {
                        InkCanvas.SetLeft(this._currentShape, currentPoint.X);
                    }
                    this._currentShape.Width = Math.Abs(width);
                    this._currentShape.Height = Math.Abs(height);
                    break;
                default:
                    throw new ApplicationException("Не известный тип фигуры");
            }
        }

        /// <summary>
        /// Завершенте фигуры
        /// </summary>
        /// <param name="endPoint">Конечная точка фигуры</param>
        public void EndDrawindShape(Point endPoint)
        {
            if (this._currentShape == null) { return; }

            //Особое отношение к линии в WPF. Нормализация.
            //Спасибо MS за пару дней потраченных на рисование ЛИНИИ

            if (this._drawMode == BOADR_DRAW_SHAPE.LINE)
            {
                Line line = (Line)this._currentShape;
                double xCanvas = Math.Min(line.X1, line.X2);
                double yCanvas = Math.Min(line.Y1, line.Y2);
                InkCanvas.SetLeft(line, xCanvas - line.StrokeThickness / 2);
                InkCanvas.SetTop(line, yCanvas - line.StrokeThickness / 2);

                line.X1 = this._beginPoint.X - xCanvas;
                line.Y1 = this._beginPoint.Y - yCanvas;
                line.X2 = endPoint.X - xCanvas;
                line.Y2 = endPoint.Y - yCanvas;
                line.Stretch = Stretch.Fill;

                //Удаление коротких линий

                if (Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2)) < 10) 
                {
                    this._editor.inkBoard.Children.Remove(this._currentShape);
                }
            }
            else if (this._drawMode == BOADR_DRAW_SHAPE.POLYLINE)
            {
                Polyline pline = (Polyline)this._currentShape;
                var xmin = (from p in pline.Points select p.X).Min();
                var ymin = (from p in pline.Points select p.Y).Min();
                var xmax = (from p in pline.Points select p.X).Max();
                var ymax = (from p in pline.Points select p.Y).Max();
                InkCanvas.SetLeft(pline, xmin - pline.StrokeThickness / 2);
                InkCanvas.SetTop(pline, ymin - pline.StrokeThickness / 2);
                pline.Stretch = Stretch.Fill;

                //Удаление коротких полилиний

                if (Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2)) < 10)
                {
                    this._editor.inkBoard.Children.Remove(this._currentShape);
                }
            }

            //Удаление маленьких фигур

            if (this._currentShape.Width < 5 && this._currentShape.Height < 5)
            {
                this._editor.inkBoard.Children.Remove(this._currentShape);
            }
            this._isDrawing = false;
            this._currentShape = null;

            this._editor.SaveState();
        }

        #endregion

        #region РЕДАКТИРОВАНИЕ

        /// <summary>
        /// Изменить параметры выдеденных графических объектов на холсте при выборе в панелях инсрументов
        /// </summary>
        /// <param name="sender">Источник события изменений (элементы панели инструментов параметров)</param>
        public void ChangeSelectedShape(object sender)
        {
            if (this._editor.inkBoard == null) { return; }

            if (sender is BrushComboBox)
            {
                string comboName = (sender as BrushComboBox).Name;
                switch (comboName)
                {
                    case "cbStrokeColor":
                        this.ChangeStokeColor();
                        break;
                    case "cbFillColor":
                        this.ChangeFillColor();
                        break;
                    default:
                        break;
                }
            }
            else if (sender is ThicknessComboBox)
            {
                this.ChangeThickness();
            }
            else if (sender is DashComboBox)
            {
                this.ChangeDash();
            }
            else if (sender is FontComboBox)
            {
                this.ChangeFontFamily();
            }
            else if (sender is ComboBox && ((ComboBox)sender).Name == "cbFontSize")
            {
                this.ChangeFontSize();
            }
        }

        /// <summary>
        /// Изменить цвет контура выделенных объектов
        /// </summary>
        public void ChangeStokeColor()
        {
            if (this._editor.inkBoard == null) { return; }

            //Штрихи

            if (this._editor.inkBoard.GetSelectedStrokes().Count != 0)
            {
                foreach (Stroke item in this._editor.inkBoard.GetSelectedStrokes())
                {
                    item.DrawingAttributes.Color = ((SolidColorBrush)this._editor.cbStrokeColor.SelectedItem).Color;
                }
                this._editor.SaveState();
            }

            //Фигуры

            if (this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    if (item is Shape)
                    {
                        ((Shape)item).Stroke = (SolidColorBrush)this._editor.cbStrokeColor.SelectedItem;
                    }
                    else if (item is LabelTextBox)
                    {
                        ((LabelTextBox)item).Foreground = (SolidColorBrush)this._editor.cbStrokeColor.SelectedItem;
                    }
                }
                this._editor.SaveState();
            }

            //Надпись (Foreground)

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this._editor) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.Foreground = (SolidColorBrush)this._editor.cbStrokeColor.SelectedItem;
                this._editor.SaveState();
            }
        }

        /// <summary>
        /// Изменить цвет заливки выделенных объектов
        /// </summary>
        public void ChangeFillColor()
        {
            if (this._editor.inkBoard == null) { return; }

            //Фигуры
            if (this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    if (item is Rectangle || item is Ellipse)
                    {
                        ((Shape)item).Fill = (SolidColorBrush)this._editor.cbFillColor.SelectedItem;
                    }
                    else if (item is LabelTextBox)
                    {
                        ((LabelTextBox)item).Background = (SolidColorBrush)this._editor.cbFillColor.SelectedItem;
                    }
                }
                this._editor.SaveState();
            }

            //Надпись

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this._editor) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.Background = (SolidColorBrush)this._editor.cbFillColor.SelectedItem;
                this._editor.SaveState();
            }
        }

        /// <summary>
        /// Изменить толщину контуров выделенных объектов
        /// </summary>
        public void ChangeThickness()
        {
            if (this._editor.inkBoard == null) { return; }

            //Штрихи

            if (this._editor.inkBoard.GetSelectedStrokes().Count != 0)
            {
                foreach (Stroke item in this._editor.inkBoard.GetSelectedStrokes())
                {
                    item.DrawingAttributes.Width = (double)this._editor.cbThickness.SelectedItem;
                    item.DrawingAttributes.Height = (double)this._editor.cbThickness.SelectedItem;
                }
                this._editor.SaveState();
            }

            //Фигуры

            if(this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    Shape shape = item as Shape;
                    if (shape != null)
                    {
                        shape.StrokeThickness = (double)this._editor.cbThickness.SelectedItem;
                    }
                }
                this._editor.SaveState();
            }
        }

        /// <summary>
        /// Изменить тип контура выделенных объектов
        /// </summary>
        public void ChangeDash()
        {
            if (this._editor.inkBoard == null) { return; }

            //Фигуры

            if (this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    Shape shape = item as Shape;
                    if (shape != null)
                    {
                        shape.StrokeDashArray = (DoubleCollection)this._editor.cbDash.SelectedItem;
                    }
                }
                this._editor.SaveState();
            }
        }

        /// <summary>
        /// Изменить шрифт выделенным надписям
        /// </summary>
        public void ChangeFontSize()
        {
            if (this._editor.inkBoard == null) { return; }

            //Выделенные надписи

            if (this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    LabelTextBox label = item as LabelTextBox;
                    if (label != null)
                    {
                        label.FontSize = Double.Parse((this._editor.cbFontSize.SelectedItem as ComboBoxItem).Content.ToString());
                        this._editor.SaveState();
                    }
                }
            }

            //Редактируемая надпись

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this._editor) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.FontSize = Double.Parse(((this._editor.cbFontSize as ComboBox).SelectedItem as ComboBoxItem).Content.ToString());
                this._editor.SaveState();
            }
        }

        /// <summary>
        /// Изменить размер шрифта выделенным надписям
        /// </summary>
        public void ChangeFontFamily()
        {
            if (this._editor.inkBoard == null) { return; }

            //Выделенные надписи

            if (this._editor.inkBoard.GetSelectedElements().Count != 0)
            {
                foreach (UIElement item in this._editor.inkBoard.GetSelectedElements())
                {
                    LabelTextBox label = item as LabelTextBox;
                    if (label != null)
                    {
                        label.FontFamily = (FontFamily)this._editor.cbFontFamaly.SelectedItem;
                        this._editor.SaveState();
                    }
                }
            }

            //Редактируемая надпись

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this._editor) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.FontFamily = (FontFamily)this._editor.cbFontFamaly.SelectedItem;
                this._editor.SaveState();
            }
        }

        #endregion
    }
}