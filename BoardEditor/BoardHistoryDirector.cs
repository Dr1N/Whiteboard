using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using BoardControls;

namespace BoardEditor
{
    /// <summary>
    /// Класс управления коллекцией досок и состоянем редактора
    /// Очередное издевательство над ООП
    /// </summary>
    class BoardHistoryDirector
    {
        readonly private int _maxBoards = 100;

        public int BoardCount
        {
            get
            {
                return this._boardHistory.Count;
            }
        }
        public int CurrentBoard
        {
            get
            {
                return this._currentBoard;
            }
        }

        private Editor _editor;
        private List<string> _boardHistory = new List<string>();
        private int _currentBoard;

        public BoardHistoryDirector(Editor editor)
        {
            this._editor = editor;
            this._currentBoard = 0;
            this._boardHistory.Add("");
        }

        public void NewBoard()
        {
            if (_boardHistory.Count > this._maxBoards)
            {
                MessageBox.Show("Достигнуто максиальное количество досок", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.SaveBoard();                                   //Сохранить текущую
            this.ClearBoard();                                  //Очистить доску
            this._boardHistory.Add("");                         //Новое состояние доски в конец списка
            this._currentBoard = this._boardHistory.Count - 1;  //Установка текущей позиции
            this.ShowBoardsInList();
            this.UpdateUI();                                    //Обновить интерфейс редактора
        }

        public void SaveBoard()
        {
            string xamlBoard = XamlWriter.Save(this._editor.inkBoard);
            this._boardHistory[this._currentBoard] = xamlBoard;
        }
       
        public void LoadBoard(int index)
        {
            this.ClearBoard();

            this._currentBoard = index;
            string state = this._boardHistory[this._currentBoard];

            StringReader stringReader = new StringReader(state);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            InkCanvas target = (InkCanvas)XamlReader.Load(xmlReader);

            foreach (UIElement item in target.Children)
            {
                if (item is BoardTextBox)
                {
                    this._editor.tbBoard.Text = (item as BoardTextBox).Text;
                }
                else
                {
                    this._editor.inkBoard.Children.Add(XamlClone<UIElement>(item));
                }
            }

            this._editor.inkBoard.Strokes = target.Strokes;
            this.UpdateUI();
        }

        public void DeleteBoаrds(int index)
        {
            this.ClearBoard();                                                          //Очищаем текущую доску
            this._boardHistory.RemoveAt(index);                                         //Удаляем текущую доску из списка
            this.LoadBoard(this._currentBoard == 0 ? 0 : this._currentBoard - 1);       //Переходим на предыдущую или остаёмся на первой(индекс 0)
            this.UpdateUI();                                                            //Обновляем интерфейс
        }

        public void GetFirstBoard()
        {
            if (this._currentBoard == 0) { return; }

            this.SaveBoard();
            this.LoadBoard(0);
        }

        public void GetLastBoard()
        {
            if (this._currentBoard == this._boardHistory.Count - 1) { return; }

            this.SaveBoard();
            this.LoadBoard(this._boardHistory.Count - 1);
        }

        public void GetNextBoard()
        {
            if (this._currentBoard >= this._boardHistory.Count - 1){ return; }

            this.SaveBoard();
            this.LoadBoard(this._currentBoard + 1);
        }

        public void GetPrevBoard()
        {
            if (this._currentBoard == 0) { return; }

            this.SaveBoard();
            this.LoadBoard(this._currentBoard - 1);
        }

        public void ShowBoardsInList()
        {
            this._editor.lbBoards.Items.Clear();
            ListBoxItem lbi;
            for (int i = 0; i < this.BoardCount; i++)
            {
                lbi = new ListBoxItem();
                lbi.Content = String.Format("Board:\t{0}", i);
                lbi.Tag = i;
                this._editor.lbBoards.Items.Add(lbi);
            }

            this._editor.lbBoards.SelectedIndex = this.CurrentBoard;
        }

        private void ClearBoard()
        {
            this._editor.tbBoard.Text = "";
            this._editor.inkBoard.Strokes.Clear();
            this._editor.ClearShapesFromBoard();
        }

        private void UpdateUI()
        {
            this._editor.ShowShapesInList();
            this._editor.lbBoards.SelectedIndex = this._currentBoard;
            this._editor.tbCurrentBoard.Text = this._currentBoard.ToString();
        }
        
        private T XamlClone<T>(T source)
        {
            string savedObject = System.Windows.Markup.XamlWriter.Save(source);

            StringReader stringReader = new StringReader(savedObject);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            T target = (T)XamlReader.Load(xmlReader);

            return target;
        }
    }
}