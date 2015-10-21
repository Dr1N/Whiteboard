using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BoardClient
{
    /// <summary>
    /// Логика взаимодействия для SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        private Regex _ipReg = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9‌​]{2}|2[0-4][0-9]|25[0-5])$");

        private string IP { get; set; }
        private int Port { get; set; }

        public SettingWindow()
        {
            InitializeComponent();
            this.cpText.SelectedColorChanged += cpText_SelectedColorChanged;
            this.cpBackground.SelectedColorChanged += cpBackground_SelectedColorChanged; 
        }

        private void cpText_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            Client client = this.Owner as Client;
            if (client == null) { return; }

            client.board.Foreground = new SolidColorBrush(this.cpText.SelectedColor);
        }

        private void cpBackground_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            Client client = this.Owner as Client;
            if (client == null) { return; }

            client.board.Background = new SolidColorBrush(this.cpBackground.SelectedColor);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (this.DialogResult == true)
            {
                e.Cancel = this.Port < 1024 || this.Port > 65535 || !this._ipReg.IsMatch(this.IP);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (this.DialogResult == true)
            {
                Client client = this.Owner as Client;
                if (client == null) { return; }

                client.Opacity = this.slOpacity.Value;
                client.Topmost = (bool)this.cbTopmost.IsChecked;
                client.ServerIp = this.tbIp.Text;
                client.ServerPort = Int32.Parse(this.tbPort.Text);
                client.Rate = (int)this.slUpdateRate.Value;
                client.ServerIp = this.tbIp.Text;
                client.ServerPort = Int32.Parse(this.tbPort.Text);
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            Client client = this.Owner as Client;
            if (client == null) { return; }

            this.slOpacity.Value = client.Opacity;
            this.cbTopmost.IsChecked = client.Topmost;
            this.tbIp.Text = client.ServerIp;
            this.tbPort.Text = client.ServerPort.ToString();
            this.slUpdateRate.Value = client.Rate;
            this.spAdress.IsEnabled = !client.mnConnect.IsChecked;
            this.slUpdateRate.IsEnabled = !client.mnConnect.IsChecked;

            this.cpText.SelectedColor = ((SolidColorBrush)client.board.Foreground).Color;
            this.cpBackground.SelectedColor = ((SolidColorBrush)client.board.Background).Color;
        }

        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void tbIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.tbIp.Background = this._ipReg.IsMatch(this.tbIp.Text) ? Brushes.White : new SolidColorBrush(Colors.Red) { Opacity = 0.3 };
            this.IP = this.tbIp.Text;
        }

        private void tbPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            int port = 0;
            Int32.TryParse(this.tbPort.Text, out port);
            this.tbPort.Background = port < 1024 || port > 65535 ? new SolidColorBrush(Colors.Red) { Opacity = 0.3 } : Brushes.White;
            this.Port = port;
        }

        private void slOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.Owner != null)
            {
                this.Owner.Opacity = e.NewValue;
            }
        }
    }
}