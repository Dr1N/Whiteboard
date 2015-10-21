using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using BoardControls;
using System.Configuration;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;

using DNBSoft.WPF;

namespace BoardClient
{
    /// <summary>
    /// Логика взаимодействия для Client.xaml
    /// </summary>
    public partial class Client : Window
    {
        #region ПОЛЯ
        
        private System.Windows.Threading.DispatcherTimer _timer;
        private BoardEditor.IBoardService _boardService;
        private string _ipServer = "127.0.0.1";
        private int _port = 17777;
        private int _updateInterval = 50;

        #endregion

        #region СВОЙСТВА
        
        public string ServerIp
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
        public int ServerPort
        {
            get
            {
                return this._port;
            }
            set
            {
                this._port = value;
            }
        }
        public int Rate
        {
            get
            {
                return this._updateInterval;
            }
            set
            {
                this._updateInterval = value;
            }
        }
        public int UpdateTime
        {
            get
            {
                return this._updateInterval;
            }
            set
            {
                this._updateInterval = value;
            }
        }
       
        #endregion

        #region КОНСТРУТОР

        public Client()
        {
            InitializeComponent();

            //Чтение из реестра

            RegistryHelper regHelper = new RegistryHelper(this);
            regHelper.LoadSetting();

            //Рамка для изменения размера

            WindowResizer wr = new WindowResizer(this);

            wr.addResizerDown(this.rsBottom);
            wr.addResizerUp(this.rsTop);
            wr.addResizerRight(this.rsRigth);
            wr.addResizerLeft(this.rsLeft);

            //Обновление доски с сервера

            this._timer = new System.Windows.Threading.DispatcherTimer();
            this._timer.Interval = TimeSpan.FromMilliseconds(this._updateInterval);
            this._timer.Tick += Update;

            //Получение IP из конфига

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

            if (ipAddr != null && portAddr > 1024 && portAddr < 65535)
            {
                this._ipServer = ipAddr.ToString();
                this._port = portAddr;
            }
        }

        #endregion

        #region СОБЫТИЯ ЭУ

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Запись в реестр

            RegistryHelper regHelper = new RegistryHelper(this);
            regHelper.SaveSettings();

            base.OnClosing(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            try
            {
                if (this.WindowState == System.Windows.WindowState.Maximized)
                {
                    this.imMaxRestore.Source = new BitmapImage(new Uri(@"Images/Restore.png", UriKind.RelativeOrAbsolute));
                    this.btMaxRestore.Command = SystemCommands.RestoreWindowCommand;
                }
                else
                {
                    this.imMaxRestore.Source = new BitmapImage(new Uri(@"Images/Maximize.png", UriKind.RelativeOrAbsolute));
                    this.btMaxRestore.Command = SystemCommands.MaximizeWindowCommand;
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

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            try
            {
                this.CreateChanel();
                this.Title = "Student Board [Подключен]";
                this._timer.Interval = TimeSpan.FromMilliseconds(this._updateInterval);
                this._timer.Start();
                this.mnConnect.IsChecked = true;
            }
            catch (Exception ex) 
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                this._timer.Stop();
                this.Title = "Student Board [Отключен]";
                this.mnConnect.IsChecked = false;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            try
            {
                this.caption.Background = SystemColors.ActiveCaptionBrush;
                this.border.BorderBrush = new SolidColorBrush(SystemColors.ActiveBorderColor);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            try
            {
                this.caption.Background = SystemColors.InactiveCaptionBrush;
                this.border.BorderBrush = new SolidColorBrush(SystemColors.InactiveBorderColor);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        private void mnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingWindow sw = new SettingWindow() { Owner = this };
                if (sw.ShowDialog() == true)
                {
                    RegistryHelper regHelper = new RegistryHelper(this);
                    regHelper.SaveSettings();
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

        private void mnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem menuItem = sender as MenuItem;
                if (menuItem == null) { return; }

                if (menuItem.IsChecked == false)
                {
                    this._timer.Stop();
                    this.Title = "Student Board [Отколючен]";
                }
                else
                {
                    this.CreateChanel();
                    this.Title = "Student Board [Подключен]";
                    this._timer.Interval = TimeSpan.FromMilliseconds(this._updateInterval);
                    this._timer.Start();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
                this._timer.Stop();
                this.Title = "Student Board [Отключен]";
                this.mnConnect.IsChecked = false;
            }
        }

        private void mnAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window about = new Window()
                {
                    Title = "О программе",
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Width = 500,
                    Height = 300,
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

        private void caption_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (this.WindowState == System.Windows.WindowState.Maximized)
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                }
                else if (this.WindowState == System.Windows.WindowState.Normal)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                }
            }
        }

        private void DragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left) { this.DragMove(); }
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

        #region WCF

        /// <summary>
        /// Создать канал WCF для получения данных
        /// </summary>
        private void CreateChanel()
        {
            string wcfAddress = String.Format("net.tcp://{0}:{1}/BoardEditor", this.ServerIp, this.ServerPort);
            NetTcpBinding ntcpb = new NetTcpBinding(SecurityMode.None) { MaxReceivedMessageSize = 262144 };
            ChannelFactory<BoardEditor.IBoardService> chanelFactory = new ChannelFactory<BoardEditor.IBoardService>
            (
               ntcpb,
               new EndpointAddress(wcfAddress)
            );
            this._boardService = chanelFactory.CreateChannel();
        }

        /// <summary>
        /// Получение XAML разметки доски по таймеру
        /// </summary>
        /// <param name="sender">Таймер - источник события</param>
        /// <param name="e">Аргументы события</param>
        private async void Update(object sender, EventArgs e)
        {
            byte[] state = null;
            try
            {
                state = await GetBoardAsync();
                if (state != null) 
                {
                    string xaml = this.Unzip(state);
                    this.UpdateBoard(xaml);
                }
            }
            catch (Exception ex)
            {
                this._timer.Stop();
                this.Title = "Student Board [Отключен]";
                this.mnConnect.IsChecked = false;
#if DEBUG
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
#endif
            }
        }

        /// <summary>
        /// Получить XAML разметку доски с сервиса
        /// </summary>
        /// <returns>Задача возвращающая XAML разметку</returns>
        private Task<byte[]> GetBoardAsync()
        {
            return Task<byte[]>.Run(
                new Func<byte[]>(
                    () => 
                    {
                        return this._boardService.GetBoard();
                    })
            );
        }

        /// <summary>
        /// Получить XAML разметку из запакованного массива байт
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <returns>Разархивированная строка</returns>
        private string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        #endregion

        #region КОМАНДЫ

        private void CommandBinding_CanCloseWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void CommandBinding_CanMaximizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(this.WindowState == System.Windows.WindowState.Maximized);
        }

        private void CommandBinding_MaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void CommandBinding_CanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void CommandBinding_CanRestoreWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.WindowState == System.Windows.WindowState.Maximized;
        }

        private void CommandBinding_RestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        #endregion

        #region ХЕЛПЕРЫ
        
        private T XamlClone<T>(T source)
        {
            string savedObject = System.Windows.Markup.XamlWriter.Save(source);

            StringReader stringReader = new StringReader(savedObject);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            T target = (T)XamlReader.Load(xmlReader);

            return target;
        }

        private void UpdateBoard(string xamlBoard)
        {
            try
            {
                StringReader stringReader = new StringReader(xamlBoard);
                XmlReader xmlReader = XmlReader.Create(stringReader);
                InkCanvas inkCanvas = (InkCanvas)XamlReader.Load(xmlReader);

                //Настройки доски

                //Размер

                if (inkCanvas.Width != this.canvas.Width) 
                {
                    this.canvas.Width = inkCanvas.Width;
                }
                if (inkCanvas.Height != this.canvas.Height) 
                {
                    this.canvas.Height = inkCanvas.Height;
                }

                //Текстовое поле

                BoardTextBox btb = inkCanvas.Children[0] as BoardTextBox;
                if (btb == null) { throw new ApplicationException("Текстовое поле не найдено"); }

                //Шрифт

                if (board.FontFamily != btb.FontFamily) { board.FontFamily = btb.FontFamily; }
                if (board.FontSize != btb.FontSize) { board.FontSize = btb.FontSize; }
                if (board.FontStretch != btb.FontStretch) { board.FontStretch = btb.FontStretch; }
                if (board.FontStyle != btb.FontStyle) { board.FontStyle = btb.FontStyle; }
                if (board.FontWeight != btb.FontWeight) { board.FontWeight = btb.FontWeight; }

                //Цвета (получение цветов преподователя)

                //if (board.Background != btb.Background) { board.Background = btb.Background; }
                //if (board.Foreground != btb.Foreground) { board.Foreground = btb.Foreground; }

                //Текст

                this.board.Text = btb.Text;

                //Штрихи

                Color background = ((SolidColorBrush)this.board.Background).Color;
                Color newColor = new Color() { A = 255, R = (byte)(0xFF - background.R), G = (byte)(0xFF - background.G), B = (byte)(0xFF - background.B) };

                this.canvas.Strokes = inkCanvas.Strokes;
                foreach (var item in this.canvas.Strokes)
                {
                    if (item.DrawingAttributes.Color == background)
                    {
                        item.DrawingAttributes.Color = newColor;
                    }
                }

                //Фигуры

                this.canvas.Children.RemoveRange(1, this.canvas.Children.Count - 1);
                for (int i = 1; i < inkCanvas.Children.Count; i++)
                {
                    #region ППЦ НА СКОРУЮ РУКУ. НЕ ДЕЛАЙТЕ ТАК (to-do)
                    
                    //Цвет подсветки активной строки

                    if (inkCanvas.Children[i] is Rectangle && ((Rectangle)inkCanvas.Children[i]).Name == "rcHighlight")
                    {
                        Color baseColor = ((SolidColorBrush)this.board.Background).Color;
                        Color hColor = new Color();
                        hColor.A = (byte)255;
                        hColor.R = (byte)(baseColor.R - 70);
                        hColor.G = (byte)(baseColor.G - 70);
                        hColor.B = (byte)(baseColor.G - 70);
                        SolidColorBrush hBrush = new SolidColorBrush(hColor) { Opacity = 0.2 };
                        ((Rectangle)inkCanvas.Children[i]).Fill = hBrush;
                    }
                    else
                    {
                        //Изменение цвета графики при совпадении с фоном пользователя
                        
                        if (inkCanvas.Children[i] is Shape)
                        {
                            Shape currentShape = inkCanvas.Children[i] as Shape;
                            
                            Color stroke = ((SolidColorBrush)currentShape.Stroke).Color;
                            if (stroke == background)
                            {
                                ((Shape)inkCanvas.Children[i]).Stroke = new SolidColorBrush(newColor);
                            }

                            if ((SolidColorBrush)currentShape.Fill != null)
                            {
                                Color fill = ((SolidColorBrush)currentShape.Fill).Color;
                                if (fill == background)
                                {
                                    ((Shape)inkCanvas.Children[i]).Fill = new SolidColorBrush(newColor);
                                }
                            }
                        }

                        //Изменение цвета надписи при совпадении с фоном пользователя

                        else if (inkCanvas.Children[i] is LabelTextBox)
                        {
                            LabelTextBox currentLabel = inkCanvas.Children[i] as LabelTextBox;

                            Color fore = ((SolidColorBrush)currentLabel.Foreground).Color;
                            Color back = ((SolidColorBrush)currentLabel.Background).Color;

                            if (fore == background)
                            {
                                ((LabelTextBox)inkCanvas.Children[i]).Foreground = new SolidColorBrush(newColor);
                            }
                            if (back == background)
                            {
                                ((LabelTextBox)inkCanvas.Children[i]).Background = new SolidColorBrush(newColor);
                            }
                        }
                    }
                    this.canvas.Children.Add(XamlClone<UIElement>(inkCanvas.Children[i]));
                    #endregion
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
#endif
            }
        }

        #endregion
    }

    ///WCF service
    namespace BoardEditor
    {
        [ServiceContract]
        interface IBoardService
        {
            [OperationContract]
            byte[] GetBoard();
        }
    }
}