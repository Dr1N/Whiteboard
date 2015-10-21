using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ColorFont;
using System.Threading;

namespace BoardEditor { 

    /// <summary> 
    /// Редактор для доски /// 
    /// </summary> 
    public partial class MainWindow : Window
    {
        #region КОНСТАНТЫ

        private readonly double _RECT_RADIUS = 15.0;

        #endregion

        #region ПОЛЯ РИСОВАНИЕ

        private bool _isDrawing;                //Рисуется ли фигура 
        private Point _beginPoint;              //Точка начала рисования 
        private Shape _currentShape;            //Рисуемая фигура 
        private BOADR_DRAW_SHAPE _drawMode;     //Текущий режим редактирования

        #endregion

        #region ВСПОМОГАТЕЛЬНЫЕ

        private Dictionary<InkCanvasEditingMode, string> _inkModesDictinary = new Dictionary<InkCanvasEditingMode, string>()
        {
            { InkCanvasEditingMode.None, "Инструмент: Ввод текста" },
            { InkCanvasEditingMode.EraseByPoint, "Инструмент: Ластик для заметок (удаляет только рукописные заметки)" },
            { InkCanvasEditingMode.EraseByStroke, "Инструмент: Удаление заметок (удаляет только рукописные заметки)" },
            { InkCanvasEditingMode.Ink, "Инструмент: Рукописные заметки" },
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

        private LabelTextBox _focusedLabel;

        #endregion

        #region КОНСТРУКТОР

        public MainWindow() 
        { 
            InitializeComponent();

            this.cbFontFamaly.SelectedItem = new FontFamily("Courier New");
        }

        #endregion

        #region СОБЫТИЯ ЭЛЕМЕНТОВ УПРАВЛЕНИЯ

        #region ПАНЕЛИ ИНСТРУМЕНТОВ

        //При получении фокуса в поле ввода включаем режим ввода текста

        private void tbBoard_GotFocus(object sender, RoutedEventArgs e)
        {
            this.inkBoard.EditingMode = InkCanvasEditingMode.None;
            this.rbType.IsChecked = true;
        }

        #region ВЫБОР ИНСТРУМЕНТА, УДАЛЕНИЕ ОБЪЕКТОВ
        
        //Режим рисования находится в свойстве Tag переключателя на панели инструментов (XAML)
        
        private void RadioButton_Checked(object sender, RoutedEventArgs e) 
        { 
            RadioButton currentRadioButton = sender as RadioButton; 
            if (currentRadioButton == null || currentRadioButton.Tag == null) { return; }
            try
            {
                this.SetShape(currentRadioButton.Tag);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Установка типа фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Удаление определённых типов объектов с доски

        private void btDelete_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null || clickedButton.Name == null) { return; }

            try
            {
                switch (clickedButton.Name)
                {
                    case "btDeleteText":
                        this.tbBoard.Text = "";
                        break;
                    case "btDeleteNotes":
                        this.inkBoard.Strokes.Clear();
                        break;
                    case "btDeleteShapes":
                        this.ClearShapesFromBoard();
                        break;
                    case "btDeleteAll":
                        this.tbBoard.Text = "";
                        this.ClearShapesFromBoard();
                        this.inkBoard.Strokes.Clear();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Удаление объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ПАНЕЛЬ НАСТРОЙКИ РИСОВАНИЯ/ШРИФТА

        private void cbStrokeColor_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (this.cbStrokeColor.SelectedItem == null) { return; }

            try
            {
                //Цвет заметок

                if (this.inkBoard.EditingMode == InkCanvasEditingMode.Ink)
                {
                    this.inkBoard.DefaultDrawingAttributes.Color = ((SolidColorBrush)this.cbStrokeColor.SelectedItem).Color;
                }

                //Изменить выделенные фигуры

                this.ChangeSelectedShape(sender);

                //Вернуть фокус полю ввода надписи

                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Изменение цвета контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbFillColor_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (this.cbFillColor.SelectedItem == null) { return; }

            try
            {
                this.ChangeSelectedShape(sender);

                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Изменение цвета заливки", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cbThickness.SelectedItem == null) { return; }

            try
            {
                //Толщина заметок

                if (this.inkBoard.EditingMode == InkCanvasEditingMode.Ink)
                {
                    this.inkBoard.DefaultDrawingAttributes.Width = (double)this.cbThickness.SelectedItem * 2;
                    this.inkBoard.DefaultDrawingAttributes.Height = (double)this.cbThickness.SelectedItem * 2;
                }

                //Изменить выделенные фигуры

                this.ChangeSelectedShape(sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Изменение толщины контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbDash_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cbDash.SelectedItem == null) { return; }
            try
            {
                this.ChangeSelectedShape(sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Изменение типа контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cbFontFamaly.SelectedItem == null) { return; }

            try
            {
                this.ChangeSelectedShape(sender);
                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Изменение шрифта", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbSetting_GotFocus(object sender, RoutedEventArgs e)
        {
            var fe = FocusManager.GetFocusedElement(this);
            this._focusedLabel = FocusManager.GetFocusedElement(this) as LabelTextBox;
        }

        #endregion

        #endregion

        #region ХОЛСТ
        
        private void inkBoard_MouseDown(object sender, MouseButtonEventArgs e) 
        { 
            if (this._drawMode == BOADR_DRAW_SHAPE.NONE) { return; }

            if (e.ChangedButton == MouseButton.Left) 
            {
                try
                {
                    this.BeginDrawingShape(e.GetPosition(this.inkBoard));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message, "Начало рисования фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } 
        } 
        
        private void inkBoard_MouseMove(object sender, MouseEventArgs e) 
        {
            if (this._drawMode != BOADR_DRAW_SHAPE.NONE || this.inkBoard.EditingMode != InkCanvasEditingMode.None)
            {
                this.sbCoord.Text = String.Format("Координаты: {0}", e.GetPosition(this.inkBoard));
            }
            
            if (this._isDrawing) 
            {
                try
                {
                    this.DrawingShape(e.GetPosition(this.inkBoard));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message, "Рисование фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } 
        }
        
        private void inkBoard_MouseUp(object sender, MouseButtonEventArgs e) 
        { 
            if (e.ChangedButton == MouseButton.Left) 
            {
                try
                {
                    this.EndDrawindShape(e.GetPosition(this.inkBoard));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message, "Завершение рисования фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } 
        }

        private void inkBoard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (this._drawMode != BOADR_DRAW_SHAPE.NONE)
            {
                Mouse.OverrideCursor = Cursors.Cross;
            }
        }

        private void inkBoard_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void inkBoard_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                //Не выделять основное текстовое поле

                if (this.inkBoard.GetSelectedElements().Count == 1 && (this.inkBoard.GetSelectedElements()[0] is BoardTextBox))
                {
                    this.inkBoard.Select(null, null);
                }

                //Панель инстурментов

                this.tbShapeSetting.IsEnabled = this.inkBoard.GetSelectedElements().Count != 0 || this.inkBoard.GetSelectedStrokes().Count != 0;
                this.tbFontSetting.IsEnabled = this.inkBoard.GetSelectedElements().Count != 0 || this.inkBoard.GetSelectedStrokes().Count != 0;

                //Выделение в списке
                if (this.inkBoard.IsFocused)
                {
                    this.SelectShapeInList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Выделение объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void inkBoard_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                this.ShowShapesInList();
            }
        }
       
        #endregion

        #region МЕНЮ
        
        private void mnExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти?", "Внимание", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                this.Close();
            }
        }

        private void mnSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingWindow sw = new SettingWindow() { Owner = this };
                sw.BoardFontInfo = FontInfo.GetControlFont(this.tbBoard);
                sw.BoardBackground = this.tbBoard.Background;

                if (sw.ShowDialog() == true)
                {
                    FontInfo.ApplyFont(this.tbBoard, sw.BoardFontInfo);
                    this.tbBoard.Background = sw.BoardBackground;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message, "Применение настроек", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void mnPanel_Click(object sender, RoutedEventArgs e)
        {
            if (mnPanel.IsChecked == true)
            {
                this.spRight.Visibility = System.Windows.Visibility.Visible;
                this.Width = this.Width + this.spRight.Width;
                this.ShowShapesInList();
            }
            else
            {
                this.spRight.Visibility = System.Windows.Visibility.Collapsed;
                this.Width = this.Width - this.spRight.Width;
            }
        }

        private void mnTest_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Test...");
            Console.WriteLine("Strokes");

            StrokeCollection strokeColl = new StrokeCollection();
            String boardText = this.tbBoard.Text;
            List<UIElement> uiColl = new List<UIElement>();

            foreach (Stroke item in this.inkBoard.Strokes)
            {
                strokeColl.Add(item.Clone());
                Console.WriteLine("Ink:\t{0}", item.GetHashCode());
            }
            foreach (UIElement item in this.inkBoard.Children)
            {
                if(item is BoardTextBox){ continue; }
                

                Console.WriteLine("Coll:\t{0}", item.GetHashCode());
            }

            Console.Write("Очистить...");

            this.inkBoard.Strokes.Clear();
            this.tbBoard.Text = "";

            Console.WriteLine("Обновить");

            this.inkBoard.Strokes = strokeColl;
        }

        #endregion

        #region СПИСКИ

        private void lbShapes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                foreach (var item in this.lbShapes.SelectedItems)
                {
                    this.inkBoard.Children.Remove((UIElement)((ListBoxItem)item).Tag);
                }
                this.ShowShapesInList();
            }
        }

        private void lbShapes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.lbShapes.IsKeyboardFocusWithin == true)
            {
                this.ShowSelectedShapesOnCanvas();
            }
        }

        private void lbShapes_GotFocus(object sender, RoutedEventArgs e)
        {
            this.ShowSelectedShapesOnCanvas();
        }

        #endregion

        #endregion

        #region РИСОВАНИЕ

        /// <summary>
        /// Установить режим рисования согласно выбранному переключателю на панелях инструментов
        /// </summary>
        /// <param name="mode">Режим рисования (Enum - InkCanvasEditingMode или BOADR_DRAW_SHAPE)</param>
        private void SetShape(Object mode)
        {
            if (mode is InkCanvasEditingMode)                                       //Режимы InkCanvas 
            {
                this.sbTool.Text = this._inkModesDictinary[(InkCanvasEditingMode)mode];
                this.tbBoard.Focusable = true;
                this.inkBoard.EditingMode = (InkCanvasEditingMode)mode;
                this.tbFontSetting.IsEnabled = false;
                
                if (this.inkBoard.EditingMode == InkCanvasEditingMode.None)         //Ввод текста
                {
                    this.tbBoard.Focus();
                    this.tbShapeSetting.IsEnabled = false;
                    this.sbCoord.Text = "Координаты:";
                }
                else                                                                //Рисование и редактироване
                {
                    this.inkBoard.Focus();
                    this.tbShapeSetting.IsEnabled = this.inkBoard.EditingMode == InkCanvasEditingMode.Ink;
                    this.inkBoard.DefaultDrawingAttributes.Color = ((SolidColorBrush)this.cbStrokeColor.SelectedItem).Color;
                    this.inkBoard.DefaultDrawingAttributes.Width = (double)this.cbThickness.SelectedItem * 2;
                    this.inkBoard.DefaultDrawingAttributes.Height = (double)this.cbThickness.SelectedItem * 2;
                }
                this._drawMode = BOADR_DRAW_SHAPE.NONE;
            }
            else if (mode is BOADR_DRAW_SHAPE)                                      //Режимы рисования фигур
            {
                this.tbBoard.Focusable = false;
                this.inkBoard.EditingMode = InkCanvasEditingMode.None;
                this.inkBoard.Focus();
                this._drawMode = (BOADR_DRAW_SHAPE)mode;
                this.tbShapeSetting.IsEnabled = true;
                this.tbFontSetting.IsEnabled = ((BOADR_DRAW_SHAPE)mode) == BOADR_DRAW_SHAPE.LABEL;
                this.sbTool.Text = this._shapeModesDictinary[(BOADR_DRAW_SHAPE)mode];
            } 
        }

        /// <summary>
        /// Добавить надпись на холст. 
        /// Компонет основан на TextBox - поэтому тут своя атмосфера
        /// </summary>
        /// <param name="coord">Координаты надписи (правый левый угол)</param>
        private void AddLabel(Point coord)
        {
            LabelTextBox labelBox = new LabelTextBox();
            labelBox.Foreground = (Brush)this.cbStrokeColor.SelectedItem;
            labelBox.Background = (Brush)this.cbFillColor.SelectedItem;
            labelBox.ToolTip = "Двойной клик для редактирования";
            labelBox.FontFamily = (FontFamily)this.cbFontFamaly.SelectedItem;
            labelBox.FontSize = Double.Parse(((this.cbFontSize.SelectedItem as ComboBoxItem).Content.ToString()));
            InkCanvas.SetLeft(labelBox, coord.X);
            InkCanvas.SetTop(labelBox, coord.Y);
            this.inkBoard.Children.Add(labelBox);
            labelBox.Focus();
            labelBox.LostFocus += (s, e) => 
            {
                this.rbType.IsChecked = true;
                Mouse.OverrideCursor = null;
                if (labelBox.Text.Trim().Length == 0)
                {
                    this.inkBoard.Children.Remove(labelBox);
                }
            };
        }

        /// <summary>
        /// Начало рисования фигуры
        /// </summary>
        /// <param name="beginPoint">Точка начала фигуры</param>
        private void BeginDrawingShape(Point beginPoint)
        {
            if (this.inkBoard.Children.Count >= 100)
            {
                MessageBox.Show("Достигнуто максиальное колчичество фигур", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //Добавление надписи

            if (this._drawMode == BOADR_DRAW_SHAPE.LABEL)
            {
                this.AddLabel(beginPoint);
                this.ShowShapesInList();
                return;
            }

            //Создание фигуры

            this._isDrawing = true;
            this._beginPoint = beginPoint;

            switch (this._drawMode)
            {
                case BOADR_DRAW_SHAPE.POLYLINE:
                    this._currentShape = new Polyline();
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

            //Настройка фигуры

            this._currentShape.SnapsToDevicePixels = true;
            this._currentShape.Stroke = (Brush)this.cbStrokeColor.SelectedItem;
            if (this._drawMode != BOADR_DRAW_SHAPE.POLYLINE)
            {
                this._currentShape.Fill = (Brush)this.cbFillColor.SelectedItem;
            }
            this._currentShape.StrokeThickness = (double)this.cbThickness.SelectedItem;
            this._currentShape.StrokeDashArray = (((DoubleCollection)this.cbDash.SelectedItem)[0] == 0.0) ? null : (DoubleCollection)this.cbDash.SelectedItem;

            this.inkBoard.Children.Add(this._currentShape);

            this.ShowShapesInList();
        }

        /// <summary>
        /// Рисование фигуры
        /// </summary>
        /// <param name="currentPoint">Текущая координата фигуры</param>
        private void DrawingShape(Point currentPoint)
        {
            switch (this._drawMode)
            {
                case BOADR_DRAW_SHAPE.POLYLINE:
                    ((Polyline)this._currentShape).Points.Add(currentPoint);
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
        private void EndDrawindShape(Point endPoint)
        {
            //Особое отношение к линии в WPF. Нормализация.
            //Спасибо MS за пару дней потраченных на рисование ЛИНИИ

            if (this._drawMode == BOADR_DRAW_SHAPE.LINE)
            {
                Line line = (Line)this._currentShape;

                double xCanvas = Math.Min(line.X1, line.X2);
                double yCanvas = Math.Min(line.Y1, line.Y2);

                InkCanvas.SetLeft(line, xCanvas);
                InkCanvas.SetTop(line, yCanvas);

                line.X1 = this._beginPoint.X - xCanvas;
                line.Y1 = this._beginPoint.Y - yCanvas;
                line.X2 = endPoint.X - xCanvas;
                line.Y2 = endPoint.Y - yCanvas;

                line.Stretch = Stretch.Fill;
            }
            else if (this._drawMode == BOADR_DRAW_SHAPE.POLYLINE)
            { 
                Polyline pline = (Polyline)this._currentShape;
                var p1 = (from p in pline.Points select p.X).Min();
                var p2 = (from p in pline.Points select p.Y).Min();
                InkCanvas.SetLeft(pline, p1);
                InkCanvas.SetTop(pline, p2);
                pline.Stretch = Stretch.Fill;
            }
            this._isDrawing = false;
            this._currentShape = null; 
        }

        #endregion

        #region ХЕЛПЕРЫ

        /// <summary>
        /// Очистить все дочерние элементы холста, кроме основного текстового поля
        /// (LINQ не прокатил))
        /// </summary>
        private void ClearShapesFromBoard()
        {
            if (this.inkBoard == null) { return; }

            List<UIElement> shapesForRemove = new List<UIElement>();
            foreach (UIElement item in this.inkBoard.Children)
            {
                if (!(item is BoardTextBox))
                {
                    shapesForRemove.Add(item);
                }
            }
            foreach (var item in shapesForRemove)
	        {
                this.inkBoard.Children.Remove(item);
	        }
        }

        /// <summary>
        /// Изменить параметры выдеденных графических объектов на холсте при изменении настроек на панелях инсрументов
        /// </summary>
        /// <param name="sender">Источник события изменений (элементы панели инструментов настройки графики)</param>
        private void ChangeSelectedShape(object sender)
        {
            if (this.inkBoard == null) { return; }

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
        private void ChangeStokeColor()
        {
            foreach (Stroke item in this.inkBoard.GetSelectedStrokes())
            {
                item.DrawingAttributes.Color = ((SolidColorBrush)this.cbStrokeColor.SelectedItem).Color;
            }

            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                if (item is Shape)
                {
                    ((Shape)item).Stroke = (SolidColorBrush)this.cbStrokeColor.SelectedItem;
                }
                else if (item is LabelTextBox)
                {
                    ((LabelTextBox)item).Foreground = (SolidColorBrush)this.cbStrokeColor.SelectedItem;
                }
            }

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.Foreground = (SolidColorBrush)this.cbStrokeColor.SelectedItem;
            }
        }

        /// <summary>
        /// Изменить цвет заливки выделенных объектов
        /// </summary>
        private void ChangeFillColor()
        {
            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                if (item is Rectangle || item is Ellipse)
                {
                    ((Shape)item).Fill = (SolidColorBrush)this.cbFillColor.SelectedItem;
                }
                else if (item is LabelTextBox)
                {
                    ((LabelTextBox)item).Background = (SolidColorBrush)this.cbFillColor.SelectedItem;
                }
            }

            LabelTextBox focusedLabel = FocusManager.GetFocusedElement(this) as LabelTextBox;
            if (focusedLabel != null)
            {
                focusedLabel.Background = (SolidColorBrush)this.cbFillColor.SelectedItem;
            }
        }

        /// <summary>
        /// Изменить толщину контуров выделенных объектов
        /// </summary>
        private void ChangeThickness()
        {
            foreach (Stroke item in this.inkBoard.GetSelectedStrokes())
            {
                item.DrawingAttributes.Width = (double)this.cbThickness.SelectedItem;
                item.DrawingAttributes.Height = (double)this.cbThickness.SelectedItem;
            }

            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                Shape shape = item as Shape;
                if (shape != null)
                {
                    shape.StrokeThickness = (double)this.cbThickness.SelectedItem;
                }
            }
        }

        /// <summary>
        /// Изменить тип контура выделенных объектов
        /// </summary>
        private void ChangeDash()
        {
            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                Shape shape = item as Shape;
                if (shape != null)
                {
                    shape.StrokeDashArray = (DoubleCollection)this.cbDash.SelectedItem;
                }
            }
        }

        /// <summary>
        /// Изменить шрифт выделенным надписям
        /// </summary>
        private void ChangeFontSize()
        {
            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                LabelTextBox element = item as LabelTextBox;
                if (item is LabelTextBox)
                {
                    ((LabelTextBox)item).FontSize = Double.Parse((this.cbFontSize.SelectedItem as ComboBoxItem).Content.ToString());
                }
            }

            foreach (UIElement item in this.inkBoard.Children)
            {
                LabelTextBox element = item as LabelTextBox;
                if (element != null && element.IsFocused)
                {
                    element.FontSize = Double.Parse(((this.cbFontSize as ComboBox).SelectedItem as ComboBoxItem).Content.ToString());
                }
            }
        }

        /// <summary>
        /// Изменить размер шрифта выделенным надписям
        /// </summary>
        private void ChangeFontFamily()
        {
            if (this.inkBoard == null) { return; }
            foreach (UIElement item in this.inkBoard.GetSelectedElements())
            {
                if (item is LabelTextBox)
                {
                    ((LabelTextBox)item).FontFamily = (FontFamily)this.cbFontFamaly.SelectedItem;
                }
            }

            foreach (UIElement item in this.inkBoard.Children)
            {
                LabelTextBox element = item as LabelTextBox;
                if (element != null && element.IsFocused)
                {
                    element.FontFamily = (FontFamily)this.cbFontFamaly.SelectedItem;
                }
            }
        }

        /// <summary>
        /// Обобразить графические фигуры в списке
        /// </summary>
        private void ShowShapesInList()
        {
            this.lbShapes.Items.Clear();
            foreach (var item in this.inkBoard.Children)
            {
                if (!(item is BoardTextBox))
                {
                    ListBoxItem lbi = new ListBoxItem();
                    lbi.Content = ((UIElement)item).GetType().Name;
                    lbi.Tag = item;
                    this.lbShapes.Items.Add(lbi);
                }
            }
        }

        /// <summary>
        /// Выделить на холсте фигуры выбранные в списке фигур
        /// </summary>
        private void ShowSelectedShapesOnCanvas()
        {
            List<UIElement> selectedShapes = new List<UIElement>();
            foreach (ListBoxItem item in this.lbShapes.Items)
            {
                if (item.IsSelected)
                {
                    selectedShapes.Add((UIElement)item.Tag);
                }
            }

            Console.WriteLine("Выделенные фигуры в списке:{0}", selectedShapes.Count);

            this.inkBoard.Select(selectedShapes);
            this.rbSelect.IsChecked = true;
        }

        /// <summary>
        /// Выделить выделенные на холсте фигуры в списке
        /// </summary>
        private void SelectShapeInList()
        {
            this.lbShapes.SelectedItems.Clear();
            foreach (var shape in this.inkBoard.GetSelectedElements())
            {
                UIElement element = shape as UIElement;
                foreach (ListBoxItem lbi in this.lbShapes.Items)
                {
                    if (element == (UIElement)lbi.Tag)
                    {
                        this.lbShapes.SelectedItems.Add(lbi);
                    }
                }
            }
        }

        #endregion
    } 
    
    /// <summary>
    /// Тип рисуемой фигуры
    /// </summary>
    public enum BOADR_DRAW_SHAPE { NONE, POLYLINE, LINE, RECTANGLE, ROUND_RECTANGLE, ELLIPSE, LABEL } 
}