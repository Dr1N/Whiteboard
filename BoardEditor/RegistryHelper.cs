using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BoardEditor
{
    class RegistryHelper
    {
        private readonly string _key = "BoardTeacher.Net";
        private Editor _editor;

        public RegistryHelper(Editor editor)
        {
            this._editor = editor;
        }

        public void SaveSettings()
        {
            try
            {
                RegistryKey boardRegKey = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey(this._key, true);
                if (boardRegKey == null)
                {
                    boardRegKey = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey(this._key);
                }

                boardRegKey.SetValue("Width", this._editor.inkBoard.Width.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("Height", this._editor.inkBoard.Height.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("Foreground", this._editor.tbBoard.Foreground.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("Backgroung", this._editor.tbBoard.Background.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("FontFamaly", this._editor.tbBoard.FontFamily.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("FontSize", this._editor.tbBoard.FontSize.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("FontStyle", this._editor.tbBoard.FontStyle.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("FontWeight", this._editor.tbBoard.FontWeight.ToString(), RegistryValueKind.String);
                boardRegKey.SetValue("FontStretch", this._editor.tbBoard.FontStretch.ToString(), RegistryValueKind.String);
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
            RegistryKey boardRegKey = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey(this._key);
            if (boardRegKey == null) { return; }

            double inkWidth;
            double inkHeigth;
            SolidColorBrush tbForeground;
            SolidColorBrush tbBackgtound;
            FontFamily tbFontFamaly;
            double tbFontSize;
            FontStyle tbFontStyle;
            FontWeight tbFontWeight;
            FontStretch tbFontStretch;

            try
            {
                //Сырые данные из реестра

                string width = boardRegKey.GetValue("Width").ToString();
                string heigth = boardRegKey.GetValue("Height").ToString();
                string foreground = boardRegKey.GetValue("Foreground").ToString();
                string backgroung = boardRegKey.GetValue("Backgroung").ToString();
                string fontFamaly = boardRegKey.GetValue("FontFamaly").ToString();
                string fontSize = boardRegKey.GetValue("FontSize").ToString();
                string fontStyle = boardRegKey.GetValue("FontStyle").ToString();
                string fontWeight = boardRegKey.GetValue("FontWeight").ToString();
                string fontStretch = boardRegKey.GetValue("FontStretch").ToString();

                //Конвертируем в параметры

                inkWidth = Double.Parse(width);
                inkHeigth = Double.Parse(heigth);
                tbForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
                tbBackgtound = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroung));
                tbFontFamaly = new FontFamily(fontFamaly);
                tbFontSize = Double.Parse(fontSize);
                tbFontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(fontStyle);
                tbFontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(fontWeight);
                tbFontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(fontStretch);
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

            this._editor.inkBoard.Width = inkWidth;
            this._editor.inkBoard.Height = inkHeigth;
            this._editor.tbBoard.Foreground = tbForeground;
            this._editor.tbBoard.Background = tbBackgtound;
            this._editor.tbBoard.FontFamily = tbFontFamaly;
            this._editor.tbBoard.FontSize = tbFontSize;
            this._editor.tbBoard.FontStyle = tbFontStyle;
            this._editor.tbBoard.FontWeight = tbFontWeight;
            this._editor.tbBoard.FontStretch = tbFontStretch;
        }
    }
}