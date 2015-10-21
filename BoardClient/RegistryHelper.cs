using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BoardClient
{
    class RegistryHelper
    {
        private readonly string _key = "BoardStudent.Net";
        private Client _client;

        public RegistryHelper(Client client)
        {
            this._client = client;
        }

        public void SaveSettings()
        {
            try
            {
                RegistryKey clientRegKey = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey(this._key, true);
                if (clientRegKey == null)
                {
                    clientRegKey = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey(this._key);
                }

                clientRegKey.SetValue("Width", this._client.Width.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Height", this._client.Height.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Top", this._client.Top.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Left", this._client.Left.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Opacity", this._client.Opacity.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Topmost", this._client.Topmost.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Update", this._client.UpdateTime.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Foreground", this._client.board.Foreground.ToString(), RegistryValueKind.String);
                clientRegKey.SetValue("Backgroung", this._client.board.Background.ToString(), RegistryValueKind.String);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
#endif
            }
        }

        public void LoadSetting()
        {
            RegistryKey clientRegKey = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey(this._key);
            if (clientRegKey == null) { return; }

            double dWidth;
            double dHeigth;
            double dTop;
            double dLeft;
            double dOpacity;
            bool bTopmost;
            int dUpdate;
            SolidColorBrush tbForeground;
            SolidColorBrush tbBackgtound;

            try
            {
                //Сырые данные из реестра

                string width = clientRegKey.GetValue("Width").ToString();
                string heigth = clientRegKey.GetValue("Height").ToString();
                string top = clientRegKey.GetValue("Top").ToString();
                string left = clientRegKey.GetValue("Left").ToString();
                string opacity = clientRegKey.GetValue("Opacity").ToString();
                string topmost = clientRegKey.GetValue("Topmost").ToString();
                string update = clientRegKey.GetValue("Update").ToString();
                string foreground = clientRegKey.GetValue("Foreground").ToString();
                string backgroung = clientRegKey.GetValue("Backgroung").ToString();

                //Конвертируем в параметры

                dWidth = Double.Parse(width);
                dHeigth = Double.Parse(heigth);
                dTop = Double.Parse(top);
                dLeft = Double.Parse(left);
                dOpacity = Double.Parse(opacity);
                bTopmost = (topmost == "True") ? true : false;
                dUpdate = Int32.Parse(update);
                tbForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
                tbBackgtound = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroung));
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
#endif
                return;
            }

            //Присваиваем параметры доске

            this._client.Width = dWidth;
            this._client.Height = dHeigth;
            this._client.Top = dTop;
            this._client.Left = dLeft;
            this._client.Opacity = dOpacity;
            this._client.Topmost = bTopmost;
            this._client.UpdateTime = dUpdate;
            this._client.board.Foreground = tbForeground;
            this._client.board.Background = tbBackgtound;
        }
    }
}