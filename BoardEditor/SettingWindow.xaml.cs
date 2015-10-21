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
using System.Windows.Shapes;
using ColorFont;
using System.Net;

namespace BoardEditor
{
    /// <summary>
    /// Логика взаимодействия для SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        #region СВОЙСТВА - НАСТРАИВАЕМЫЕ ПАРАМЕТРЫ

        public FontInfo BoardFontInfo
        {
            get;
            set;
        }
        public Brush BoardBackground
        {
            get;
            set;
        }

        private string IP { get; set; }
        private int Port { get; set; }

        #endregion

        private Editor _editor;

        public SettingWindow()
        {
            InitializeComponent();
            this.colorPicker.SelectedColorChanged += colorPicker_SelectedColorChanged;

            //Получим IP адреса

            IPHostEntry ipEntry = Dns.GetHostEntry("");
            IPAddress[] addr = ipEntry.AddressList;
            for (int i = 0; i < addr.Length; i++)
            {
                if (!addr[i].IsIPv6LinkLocal)
                {
                    this.cbIp.Items.Add(addr[i].ToString());
                }
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            //Редактор

            this._editor = (Editor)this.Owner;
            FontInfo.ApplyFont(this.tbExample, this.BoardFontInfo);
            this.tbExample.Background = this.BoardBackground;
            this.slWidth.Value = this._editor.inkBoard.Width;
            this.slHeight.Value = this._editor.inkBoard.Height;
            this.colorPicker.SelectedColor = ((SolidColorBrush)this.BoardBackground).Color;

            //Соединение

            this.cbIp.SelectedItem = this._editor.IP;
            this.tbPort.Text = this._editor.Port.ToString();

            this.spAddress.IsEnabled = (bool)!this._editor.btPlay.IsChecked;
            this.tbHint.Text = this.spAddress.IsEnabled == false ? "*Для изменения настроек отключите трасляцию" : "";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (this.DialogResult == true)
            {
                e.Cancel = this.Port < 1024 || this.Port > 65535;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (this.DialogResult == true)
            {
                this.BoardFontInfo = FontInfo.GetControlFont(this.tbExample);
                this.BoardBackground = this.tbExample.Background;

                this._editor.inkBoard.Width = this.slWidth.Value;
                this._editor.inkBoard.Height = this.slHeight.Value;

                this._editor.IP = this.cbIp.SelectedItem.ToString();
                this._editor.Port = Int32.Parse(this.tbPort.Text.Trim());

                FontInfo.ApplyFont(this._editor.tbBoard, this.BoardFontInfo);
                this._editor.tbBoard.Background = this.BoardBackground;

                this._editor.ResetClientsUpdate();
            }
        }

        private void colorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            this.tbExample.Background = new SolidColorBrush(this.colorPicker.SelectedColor);
        }

        private void tbPort_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            int port = 0;
            Int32.TryParse(this.tbPort.Text, out port);
            this.tbPort.Background = port < 1024 || port > 65535 ? new SolidColorBrush(Colors.Red) { Opacity = 0.3 } : Brushes.White;
            this.Port = port;
        }

        private void btFont_Click(object sender, RoutedEventArgs e)
        {
            ColorFontDialog fntDialog = new ColorFontDialog();
            fntDialog.Owner = this;
            fntDialog.Font = FontInfo.GetControlFont(this.tbExample);
            if (fntDialog.ShowDialog() == true)
            {
                FontInfo selectedFont = fntDialog.Font;
                if (selectedFont != null)
                {
                    FontInfo.ApplyFont(this.tbExample, selectedFont);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}