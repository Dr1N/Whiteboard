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
using System.Threading;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.ServiceModel;
using System.Windows.Controls.Primitives;
using System.Net;
using System.ServiceModel.Channels;
using System.Configuration;
using System.IO.Compression;
using ColorFont;
using BoardControls;

namespace BoardEditor
{ 
    /// <summary> 
    /// Редактор и вещатель доски
    /// </summary> 
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single)]
    public partial class Editor : Window, IBoardService
    {   
        #region ПОЛЯ

        private readonly int _maxFrames = 100;                      //Количество сохраняемых состояний

        private LabelTextBox _focusedLabel;                         //Ссылка на надпись на которой был фокус при выборе параметров на панели инструментов
        private BoardHistoryHelper _historyHelper;                  //Класс-хелпер для управления историей досок
        private DrawingHelper _drawingHelper;                       //Класс-хелпер для рисованием
        private ServiceHost _service;                               //WCF сервис
        
        private List<string> _changesHistory;                       //История изменений текущей доски (состояния)
        public List<string> States 
        {
            get
            {
                return this._changesHistory;
            }
            set
            {
                this._changesHistory = value;
            }
        }

        private int _currentState = 0;                              //Текущее состояние(индекс)
        public int CurrentState 
        {
            get
            {
                return this._currentState;
            }
            set
            {
                this._currentState = value;
            }
        }
        
        private bool _isSaveState;                                  //Undo/Redo флаг(не сохранять состояние при отмене/возврате)

        private string _ipServer = "127.0.0.1";                     //IP трансляции
        public string IP 
        {
            get
            {
                return this._ipServer;
            }
            set
            {
                this._ipServer = value;
            }
        }
        
        private int _portServer = 17777;                            //Порт трансляции
        public int Port 
        {
            get
            {
                return this._portServer;
            }
            set
            {
                this._portServer = value;
            }
        }

        private Dictionary<string, ClientInfo> _clientsUpdate;      //Словарь флагов, получил ли клиент обновление
        private System.Windows.Threading.DispatcherTimer _cTimer;   //Таймер для удаления неактивных клиентов

        private int _caretLineBeforeChanges = -1;                   //Индекс строки, в которой находится карректа

        #endregion

        #region КОНСТРУКТОР

        public Editor() 
        {
            try
            {
                this._historyHelper = new BoardHistoryHelper(this);
                this._drawingHelper = new DrawingHelper(this);
                this._changesHistory = new List<string>();
                this._clientsUpdate = new Dictionary<string, ClientInfo>();
                this._cTimer = new System.Windows.Threading.DispatcherTimer();
                this._cTimer.Interval = TimeSpan.FromSeconds(5);
                this._cTimer.Tick += _cTimer_Tick;
                this._cTimer.Start();

                InitializeComponent();

                this.ShowBoardsInList();
                this.tbCurrentBoard.Text = this._historyHelper.CurrentBoard.ToString();
                this.cbFontFamaly.SelectedItem = new FontFamily("Courier New");

                this.SaveState();

                //Выбор IP

                IPHostEntry ipEntry = Dns.GetHostEntry("");
                IPAddress[] addr = ipEntry.AddressList;

                if(addr.Length > 0)
                {
                    this._ipServer = addr.First<IPAddress>(ip => !ip.IsIPv6LinkLocal).ToString();
                }

                string ipConf = null;
                string portConf = null;

                try
                {
                    ipConf = ConfigurationManager.AppSettings["IP"];
                    portConf = ConfigurationManager.AppSettings["Port"];
                }
                catch { }

                IPAddress ipAddr = null;
                IPAddress.TryParse(ipConf, out ipAddr);

                int portAddr = 0;
                Int32.TryParse(portConf, out portAddr);

                if (ipAddr != null && (addr.Contains(ipAddr) || IPAddress.IsLoopback(ipAddr)) && portAddr > 1024 && portAddr < 65535)
                {
                    this._ipServer = ipAddr.ToString();
                    this._portServer = portAddr;
                }

                //Прямоугольник для подсветки активной строки

                Rectangle highlightingRect = new Rectangle() { Name = "rcHighlight" };
                InkCanvas.SetLeft(highlightingRect, 0);
                InkCanvas.SetTop(highlightingRect, 0);
                highlightingRect.Width = 0;
                highlightingRect.Height = 0;
                this.inkBoard.Children.Add(highlightingRect);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Всё сломалось", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            //Чтение настроек из реестра

            try
            {
                RegistryHelper regHelper = new RegistryHelper(this);
                regHelper.LoadSetting();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        #endregion
        
        #region СОБЫТИЯ ЭЛЕМЕНТОВ УПРАВЛЕНИЯ

        #region ОКНО

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            //Создание сервиса

            try
            {
                this.CreateService();
                this.Title = String.Format("Teacher Board [Трансляция: {0}:{1}]", this._ipServer, this._portServer);
                this.btPlay.IsChecked = true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                this.Title = String.Format("Teacher Board [Отключено]");
                this.btPlay.IsChecked = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (MessageBox.Show("Выйти?", "Внимание", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (this._service != null && this._service.State == CommunicationState.Opened)
            {
                this._service.Close(TimeSpan.FromSeconds(1));
            }
        }

        #endregion

        #region ТЕКСТОВОЕ ПОЛЕ

        //При получении фокуса в поле ввода включаем режим ввода текста

        private void tbBoard_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                this.inkBoard.EditingMode = InkCanvasEditingMode.None;
                this.rbType.IsChecked = true;
                this.HighlightCurrentString();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void tbBoard_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Rectangle highlightingRect = LogicalTreeHelper.FindLogicalNode(this, "rcHighlight") as Rectangle;
                if (highlightingRect == null)
                {
                    throw new ApplicationException("Подсветка строки. Капут.");
                }
                highlightingRect.Fill = null;
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void tbBoard_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //Сохранить состояние, если не происходит отмена (Undo)

                if (this._isSaveState)
                {
                    this.SaveState();
                }
                this._isSaveState = true;

                //Сброс клиентов

                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void tbBoard_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                int currentLine = this.tbBoard.GetLineIndexFromCharacterIndex(this.tbBoard.CaretIndex);
                if (currentLine == this._caretLineBeforeChanges) { return; }
                this.HighlightCurrentString();
                this.ResetClientsUpdate();
                this._caretLineBeforeChanges = currentLine;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }
                
        #endregion

        #region ПАНЕЛИ ИНСТРУМЕНТОВ

        #region ВЫБОР ИНСТРУМЕНТА, УДАЛЕНИЕ ОБЪЕКТОВ

        //Режим рисования находится в свойстве Tag переключателя на панели инструментов (XAML)
        
        private void RadioButton_Checked(object sender, RoutedEventArgs e) 
        { 
            RadioButton currentRadioButton = sender as RadioButton;
            if (currentRadioButton == null || currentRadioButton.Tag == null) { return; }
            try
            {
                this._drawingHelper.SetShape(currentRadioButton.Tag);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Установка типа фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Удаление определённых типов объектов с доски (Тулбар и меню)

        private void btDelete_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            MenuItem clickedMenu = sender as MenuItem;

            if (clickedButton == null && clickedMenu == null) { return; }

            try
            {
                string name = clickedButton != null ? name = clickedButton.Name.Substring(2) : clickedMenu.Name.Substring(2);
                switch (name)
                {
                    case "DeleteText":
                        if (MessageBox.Show("Очистить?", "Board", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) { return; }
                        this.tbBoard.Text = "";
                        this.HighlightCurrentString();
                        break;
                    case "DeleteNotes":
                        this.inkBoard.Strokes.Clear();
                        this.ResetClientsUpdate();
                        break;
                    case "DeleteShapes":
                        if (MessageBox.Show("Очистить?", "Board", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) { return; }
                        this.ClearShapesFromBoard();
                        this.ShowShapesInList();
                        this.ResetClientsUpdate();
                        break;
                    case "DeleteAll":
                        if (MessageBox.Show("Очистить?", "Board", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) { return; }
                        this._isSaveState = false;
                        this.tbBoard.Text = "";
                        this.HighlightCurrentString();
                        this.ClearShapesFromBoard();
                        this.inkBoard.Strokes.Clear();
                        this.ShowShapesInList();
                        this.ResetClientsUpdate();
                        break;
                    default:
                        break;
                }
                this.SaveState();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Удаление объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ПАНЕЛИ ПАРАМЕТРОВ РИСОВАНИЯ/ШРИФТА

        private void cbStrokeColor_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (this.inkBoard == null || this.cbStrokeColor.SelectedItem == null) { return; }

            try
            {
                //Цвет заметок

                if (this.inkBoard.EditingMode == InkCanvasEditingMode.Ink)
                {
                    this.inkBoard.DefaultDrawingAttributes.Color = ((SolidColorBrush)this.cbStrokeColor.SelectedItem).Color;
                }

                //Изменить выделенные фигуры

                if (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0 || this._focusedLabel != null)
                {
                    this._drawingHelper.ChangeSelectedShape(sender);
                    this.ResetClientsUpdate();
                }

                //Вернуть фокус полю ввода надписи

                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Изменение цвета контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbFillColor_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (this.inkBoard == null || this.cbFillColor.SelectedItem == null) { return; }

            try
            {
                if (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0 || this._focusedLabel != null)
                {
                    this._drawingHelper.ChangeSelectedShape(sender);
                    this.ResetClientsUpdate();
                }

                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Изменение цвета заливки", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.inkBoard == null || this.cbThickness.SelectedItem == null) { return; }

            try
            {
                //Толщина заметок

                if (this.inkBoard.EditingMode == InkCanvasEditingMode.Ink)
                {
                    this.inkBoard.DefaultDrawingAttributes.Width = (double)this.cbThickness.SelectedItem * 4;
                    this.inkBoard.DefaultDrawingAttributes.Height = (double)this.cbThickness.SelectedItem * 4;
                }

                //Изменить выделенные фигуры

                if (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0 || this._focusedLabel != null)
                {
                    this._drawingHelper.ChangeSelectedShape(sender);
                    this.ResetClientsUpdate();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Изменение толщины контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbDash_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.inkBoard == null || this.cbDash.SelectedItem == null) { return; }

            try
            {
                if (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0 || this._focusedLabel != null)
                {
                    this._drawingHelper.ChangeSelectedShape(sender);
                    this.ResetClientsUpdate();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Изменение типа контура", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.inkBoard == null || this.cbFontFamaly.SelectedItem == null) { return; }

            try
            {
                if (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0 || this._focusedLabel != null)
                {
                    this._drawingHelper.ChangeSelectedShape(sender);
                    this.ResetClientsUpdate();
                }
                if (this._focusedLabel != null)
                {
                    this._focusedLabel.Focus();
                    this._focusedLabel = null;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Изменение шрифта", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbSetting_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = FocusManager.GetFocusedElement(this);
                this._focusedLabel = FocusManager.GetFocusedElement(this) as LabelTextBox;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        #endregion

        #region ПАНЕЛЬ УРАВЛЕНИЯ ДОСКАМИ

        private void btBoard_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) { return; }
            try
            {
                switch (clickedButton.Name)
                {
                    case "btNewBoard":
                        this._historyHelper.NewBoard();
                        break;
                    case "btNextBoard":
                        this._historyHelper.GetNextBoard();
                        break;
                    case "btPrevBoard":
                        this._historyHelper.GetPrevBoard();
                        break;
                    case "btFirstBoard":
                        this._historyHelper.GetFirstBoard();
                        break;
                    case "btLastBoard":
                        this._historyHelper.GetLastBoard();
                        break;
                    default:
                        break;
                }
                this.ShowShapesInList();
                this.ShowBoardsInList();
                this.HighlightCurrentString();
                this.ResetClientsUpdate();
                this.lbBoards.SelectedIndex = this._historyHelper.CurrentBoard;
                this.tbCurrentBoard.Text = this._historyHelper.CurrentBoard.ToString();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Управление досками", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton btnPlay = sender as ToggleButton;
            if (btnPlay == null) { return; }

            try
            {
                this.BoardTranslation(btnPlay);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                btnPlay.IsChecked = false;
                this.Title = "Teacher Board [Отключено]";
                MessageBox.Show(ex.Message, "WCF Сервис", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #endregion

        #region ХОЛСТ

        private void inkBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this._drawingHelper._drawMode == BOADR_DRAW_SHAPE.NONE) { return; }

            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    this._drawingHelper.BeginDrawingShape(e.GetPosition(this.inkBoard));
                    this.ShowShapesInList();
                    this.ResetClientsUpdate();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Начало рисования фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } 
        } 
        
        private void inkBoard_MouseMove(object sender, MouseEventArgs e) 
        {
            if (this._drawingHelper._drawMode != BOADR_DRAW_SHAPE.NONE || this.inkBoard.EditingMode != InkCanvasEditingMode.None)
            {
                this.sbCoord.Text = String.Format("Координаты: {0}", e.GetPosition(this.inkBoard));
            }
            
            if (this._drawingHelper._isDrawing == true) 
            {
                try
                {
                    this._drawingHelper.DrawingShape(e.GetPosition(this.inkBoard));
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
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
                    if (this._drawingHelper._isDrawing == true)
                    {
                        this._drawingHelper.EndDrawindShape(e.GetPosition(this.inkBoard));
                        this.ShowShapesInList();
                        this.ResetClientsUpdate();
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Завершение рисования фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } 
        }

        private void inkBoard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (this._drawingHelper._drawMode != BOADR_DRAW_SHAPE.NONE)
            {
                Mouse.OverrideCursor = Cursors.Cross;
            }
        }

        private void inkBoard_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
            try
            {
                if (this._drawingHelper._isDrawing == true)
                {
                    this._drawingHelper.EndDrawindShape(e.GetPosition(this.inkBoard));
                    this.ShowShapesInList();
                    this.ResetClientsUpdate();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Завершение рисования фигуры", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void inkBoard_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                //Не выделять основное текстовое поле

                if (this.inkBoard.GetSelectedElements().Count == 1 && 
                    (this.inkBoard.GetSelectedElements()[0] is BoardTextBox))
                {
                    this.inkBoard.Select(null, null);
                }

                Rectangle highlightingRect = LogicalTreeHelper.FindLogicalNode(this, "rcHighlight") as Rectangle;
                if (this.inkBoard.GetSelectedElements().Contains(highlightingRect))
                {
                    this.inkBoard.Select(null, null); //to-do
                }

                //Панель инстурментов

                this.tbShapeSetting.IsEnabled = this.inkBoard.GetSelectedElements().Count != 0 || this.inkBoard.GetSelectedStrokes().Count != 0;
                this.tbFontSetting.IsEnabled = this.inkBoard.GetSelectedElements().Count != 0 || this.inkBoard.GetSelectedStrokes().Count != 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Выделение объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void inkBoard_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            try
            {
                this.SaveState();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void inkBoard_StrokeErased(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SaveState();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void inkBoard_SelectionMoved(object sender, EventArgs e)
        {
            try
            {
                this.SaveState();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void inkBoard_SelectionResized(object sender, EventArgs e)
        {
            try
            {
                this.SaveState();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void inkBoard_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                try
                {
                    this.ShowShapesInList();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Удаление объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void inkBoard_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && (this.inkBoard.GetSelectedElements().Count > 0 || this.inkBoard.GetSelectedStrokes().Count > 0))
            {
                try
                {
                    this.ResetClientsUpdate();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Удаление объектов", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
       
        #endregion

        #region МЕНЮ
        
        private void mnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
                    RegistryHelper regHelper = new RegistryHelper(this);
                    regHelper.SaveSettings();
                    this.HighlightCurrentString();
                    this.ResetClientsUpdate();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Применение (сохранение) настроек", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void mnPanel_Click(object sender, RoutedEventArgs e)
        {
            this.spRight.Visibility = (this.mnPanel.IsChecked == true) ? this.spRight.Visibility = System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void mnAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window about = new Window()
                {
                    Title = "О программе",
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    Owner = this
                };
                about.Content = new About();
                about.ShowDialog();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void mnTest_Click(object sender, RoutedEventArgs e)
        {
            Console.Clear();
            Console.WriteLine("Test...");
        }

        #endregion

        #region СПИСКИ(ДОП ПАНЕЛЬ)

        #region ФИГУРЫ
        
        private void lbShapes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && this.lbShapes.SelectedItems.Count > 0)
            {
                try
                {
                    foreach (var item in this.lbShapes.SelectedItems)
                    {
                        this.inkBoard.Children.Remove((UIElement)((ListBoxItem)item).Tag);
                    }
                    this.ShowShapesInList();
                    this.ResetClientsUpdate();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Удаление фигур из списка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void lbShapes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.lbShapes.IsKeyboardFocusWithin == true)
            {
                try
                {
                    this.ShowSelectedShapesOnCanvas();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Выделение фигур из списка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void lbShapes_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ShowSelectedShapesOnCanvas();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                MessageBox.Show(ex.Message, "Работа со списком фигур", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ДОСКИ

        private void lbBoards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.lbBoards.SelectedIndex != -1 && this.lbBoards.IsKeyboardFocusWithin == true)
            {
                try
                {
                    this._historyHelper.SaveBoard();
                    this._historyHelper.LoadBoard(this.lbBoards.SelectedIndex);
                    this.tbCurrentBoard.Text = this._historyHelper.CurrentBoard.ToString();
                    this.ShowShapesInList();
                    this.ResetClientsUpdate();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Переключение доски в списке", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void lbBoards_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                try
                {
                    if (this._historyHelper.BoardCount <= 1)
                    {
                        MessageBox.Show("Нельзя удалить все доски", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    this._historyHelper.DeleteBoаrds(this.lbBoards.SelectedIndex);
                    this.ShowBoardsInList();
                    this.ResetClientsUpdate();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
#endif
                    MessageBox.Show(ex.Message, "Удаление досок из списка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region WCF

        /// <summary>
        /// Вернуть доску (ServiceContract)
        /// </summary>
        /// <returns>массив байт содержащий XAML разметку холста</returns>
        public byte[] GetBoard()
        {
            try
            {
                OperationContext context = OperationContext.Current;
                MessageProperties messageProperties = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

                //Клиент

                string key = String.Format("{0}:{1}", endpointProperty.Address, endpointProperty.Port);

                if (!this._clientsUpdate.ContainsKey(key)) { this._clientsUpdate[key] = new ClientInfo(); }

                //Есть ли обновления

                if (this._clientsUpdate.ContainsKey(key) && this._clientsUpdate[key].IsUpdated == true)
                {
                    this._clientsUpdate[key].LastTime = DateTime.Now;
                    return null;
                }

                //Отправка доски

                this._clientsUpdate[key].IsUpdated = true;
                this._clientsUpdate[key].LastTime = DateTime.Now;

                return this.Zip(XamlWriter.Save(this.inkBoard));
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
#endif
                return null;
            }
        }

        /// <summary>
        /// Начать/остановить трансляцию доски
        /// </summary>
        /// <param name="btnPlay">Кнопка переключатель трансляции</param>
        private void BoardTranslation(ToggleButton btnPlay)
        {
            if (btnPlay.IsChecked == true)
            {
                this.CreateService();
                this.Title = String.Format("Teacher Board [Трансляция: {0}:{1}]", this._ipServer, this._portServer);
            }
            else
            {
                this._service.Close(TimeSpan.FromSeconds(1));
                this.Title = "Teacher Board [Отключено]";
            }
        }

        /// <summary>
        /// Создание WCF сервиса согласно настроек приложения
        /// </summary>
        private void CreateService()
        {
            string wcfAddress = String.Format("net.tcp://{0}:{1}/BoardEditor", this._ipServer, this._portServer);
            this._service = new ServiceHost(this, new Uri(wcfAddress));
            NetTcpBinding ntcpb = new NetTcpBinding(SecurityMode.None) { MaxReceivedMessageSize = 262144, MaxConnections = 50 };
            this._service.AddServiceEndpoint(typeof(IBoardService), ntcpb, "");
            this._service.Open();
        }

        /// <summary>
        /// Сбросить флаг обновления для клиентов
        /// </summary>
        public void ResetClientsUpdate()
        {
            if (this._clientsUpdate == null) { return; }

            foreach (var item in this._clientsUpdate.Keys.ToArray<string>())
            {
                this._clientsUpdate[item].IsUpdated = false;
            }
        }

        /// <summary>
        /// Очистка коллекции пользователей от неактивных пользователй (по таймеру)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _cTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                List<string> badUsersKeys = new List<string>();
                foreach (var item in this._clientsUpdate)
                {
                    if ((DateTime.Now - item.Value.LastTime).Seconds > 5) { badUsersKeys.Add(item.Key); }
                }

                foreach (var item in badUsersKeys)
                {
                    this._clientsUpdate.Remove(item);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        /// <summary>
        /// Сжать строку Zip'ом и конвертировать байты в строку
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <returns>Строка из сжатого массива байты</returns>
        private byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        #endregion

        #region ХЕЛПЕРЫ

        /// <summary>
        /// Сохранить состояние доски после изменений(история)
        /// </summary>
        public void SaveState()
        {
            //Удалить от текущей позиции до конца истории
           
            for (int i = this._currentState + 1; i < this._changesHistory.Count; i++)
            {
                this._changesHistory.RemoveAt(i);
            }

            //Сохранить состояние

            this._changesHistory.Add(XamlWriter.Save(this.inkBoard));
            this._currentState = this._changesHistory.Count - 1;

            //Очистить историю при достижении лимита

            if (this._changesHistory.Count >= this._maxFrames)
            {
                this._changesHistory.RemoveRange(0, 10);
            }
        }

        /// <summary>
        /// Загрузить состояние доски
        /// </summary>
        /// <param name="state">XAML разметка доски</param>
        public void LoadState(string state)
        { 
            //Очистить доску

            this.ClearShapesFromBoard();
            this.inkBoard.Strokes.Clear();

            //Загрузить состояние

            StringReader stringReader = new StringReader(state);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            InkCanvas target = (InkCanvas)XamlReader.Load(xmlReader);

            //Фигуры

            foreach (UIElement item in target.Children)
            {
                //Загружаем всё кроме основного текстового поля и прямоугольника подсветки

                Rectangle hr = item as Rectangle;
                if (item is BoardTextBox || (hr != null && hr.Name == "rcHighlight")) 
                { 
                    continue; 
                }
                this.inkBoard.Children.Add(XamlClone<UIElement>(item));
            }
            
            //Текст

            int position = this.tbBoard.CaretIndex;
            this.tbBoard.Text = (target.Children[0] as BoardTextBox).Text;
            this.tbBoard.CaretIndex = position;

            //Штрихи

            this.inkBoard.Strokes = target.Strokes;

            //Список фигур

            this.ShowShapesInList();
        }

        /// <summary>
        /// Очистить все дочерние элементы холста, кроме основного текстового поля и прямоугольника подсветки
        /// </summary>
        public void ClearShapesFromBoard()
        {
            if (this.inkBoard == null) { return; }

            List<UIElement> shapesForRemove = new List<UIElement>();
            foreach (UIElement item in this.inkBoard.Children)
            {
                Rectangle hr = item as Rectangle;
                
                //Удаляем детей кроме основного текстового поля и прямоугольника подсветки
                
                if (item is BoardTextBox || (hr != null && hr.Name == "rcHighlight")) { continue; }
                shapesForRemove.Add(item);
            }
            foreach (var item in shapesForRemove)
            {
                this.inkBoard.Children.Remove(item);
            }
        }

        /// <summary>
        /// Очистить доску(новая доска)
        /// </summary>
        public void ClearBoard()
        {
            this.tbBoard.Text = "";
            this.inkBoard.Strokes.Clear();
            this.ClearShapesFromBoard();
        }

        /// <summary>
        /// Вывести графические фигуры в список
        /// </summary>
        private void ShowShapesInList()
        {
            this.lbShapes.Items.Clear();
            foreach (var item in this.inkBoard.Children)
            {
                Rectangle hr = item as Rectangle;
                if (!(item is BoardTextBox || (hr != null && hr.Name == "rcHighlight")))
                {
                    ListBoxItem lbi = new ListBoxItem();
                    lbi.Content = ((UIElement)item).GetType().Name;
                    lbi.Tag = item;
                    this.lbShapes.Items.Add(lbi);
                }
            }
        }

        /// <summary>
        /// Вывести доски в список
        /// </summary>
        public void ShowBoardsInList()
        {
            this.lbBoards.Items.Clear();
            ListBoxItem lbi;
            for (int i = 0; i < this._historyHelper.BoardCount; i++)
            {
                lbi = new ListBoxItem();
                lbi.Content = String.Format("Доска:\t{0}", i);
                lbi.Tag = i;
                this.lbBoards.Items.Add(lbi);
            }
            this.lbBoards.SelectedIndex = this._historyHelper.CurrentBoard;
        }

        /// <summary>
        /// Выделить на холсте фигуры выбранные в списке фигур
        /// </summary>
        private void ShowSelectedShapesOnCanvas()
        {
            List<UIElement> selectedShapes = new List<UIElement>();
            foreach (ListBoxItem item in this.lbShapes.SelectedItems)
            {
                selectedShapes.Add((UIElement)item.Tag);
            }
            this.inkBoard.Select(selectedShapes);
            this.rbSelect.IsChecked = true;
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
            T target = (T)XamlReader.Load(xmlReader);

            return target;
        }

        /// <summary>
        /// Подсветка активной строки
        /// </summary>
        private void HighlightCurrentString()
        {
            if (this.tbBoard.IsFocused == false) { return; }    //to-do

            //Положение линии

            double topLineCoord = this.tbBoard.GetRectFromCharacterIndex(this.tbBoard.CaretIndex).Top;
            double lineHeight = this.tbBoard.LineHeight;

            Rectangle highlightingRect = LogicalTreeHelper.FindLogicalNode(this, "rcHighlight") as Rectangle;
            if (highlightingRect == null)
            {
                throw new ApplicationException("Подсветка строки");
            }

            InkCanvas.SetLeft(highlightingRect, 0);
            InkCanvas.SetTop(highlightingRect, topLineCoord);
            highlightingRect.Width = this.tbBoard.ActualWidth;
            highlightingRect.Height = lineHeight;

            //Цвет выделения

            Color baseColor = ((SolidColorBrush)this.tbBoard.Background).Color;
            Color newColor = new Color();
            newColor.A = (byte)255;
            newColor.R = (byte)(baseColor.R - 70);
            newColor.G = (byte)(baseColor.G - 70);
            newColor.B = (byte)(baseColor.G - 70);
            SolidColorBrush hBrush = new SolidColorBrush(newColor) { Opacity = 0.2 };
            highlightingRect.Fill = hBrush;
        }

        #endregion
        
        #region КОМАНДЫ

        private void CommandBinding_CanUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this._currentState - 1 >= 0;
        }

        private void CommandBinding_ExecutedUndo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                this._isSaveState = false;
                this.LoadState(this._changesHistory[--this._currentState]);
                this.HighlightCurrentString();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void CommandBinding_CanRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.CurrentState < this.States.Count - 1;
        }

        private void CommandBinding_ExecutedRedo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                this._isSaveState = false;
                this.LoadState(this._changesHistory[++this._currentState]);
                this.HighlightCurrentString();
                this.ResetClientsUpdate();
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        #endregion
    } 
}