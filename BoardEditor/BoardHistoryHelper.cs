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
    class BoardHistoryHelper
    {
        private readonly int _maxBoards = 100;

        public int BoardCount
        {
            get
            {
                return this._boards.Count;
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

        private List<BoardData> _boards = new List<BoardData>();    //Коллекция состояний досок
        private int _currentBoard;                                  //Индекс активной доски

        public BoardHistoryHelper(Editor editor)
        {
            this._editor = editor;
            this._currentBoard = 0;
            this._boards.Add(new BoardData());
        }

        public void NewBoard()
        {
            if (_boards.Count > this._maxBoards)
            {
                MessageBox.Show("Достигнуто максиальное количество досок", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.SaveBoard();                                                           //Сохранить текущую
            this._editor.ClearBoard();                                                  //Очистить доску
            this._boards.Add(new BoardData(XamlWriter.Save(this._editor.inkBoard)));    //Добавить текущую доску в коллекцию
            
            this._currentBoard = this._boards.Count - 1;                                //Установка текущей позиции
            this._editor.States = this._boards[this._currentBoard].States;
            this._editor.CurrentState = this._boards[this._currentBoard].CurrentState;
        }

        public void SaveBoard()
        {
            //Доска

            string xamlBoard = XamlWriter.Save(this._editor.inkBoard);
            this._boards[this._currentBoard].BoardXaml = xamlBoard;

            //История изменений

            string[] tmpToListCopy = new string[this._editor.States.Count];
            this._editor.States.CopyTo(tmpToListCopy);
            this._boards[this._currentBoard].States = new List<string>(tmpToListCopy);
            this._boards[this._currentBoard].CurrentState = this._editor.CurrentState;
        }
       
        public void LoadBoard(int index)
        {
            //Доска

            this._currentBoard = index;
            string state = this._boards[this._currentBoard].BoardXaml;
            this._editor.LoadState(state);

            //История изменений

            this._editor.States = this._boards[this._currentBoard].States;
            this._editor.CurrentState = this._boards[this._currentBoard].CurrentState;
        }

        public void DeleteBoаrds(int index)
        {
            this._editor.ClearBoard();                                                  //Очищаем текущую доску
            this._boards.RemoveAt(index);                                               //Удаляем текущую доску из списка
            this.LoadBoard(this._currentBoard == 0 ? 0 : this._currentBoard - 1);       //Переходим на предыдущую или остаёмся на первой(индекс 0)
        }

        public void GetFirstBoard()
        {
            if (this._currentBoard == 0) { return; }

            this.SaveBoard();
            this.LoadBoard(0);
        }

        public void GetLastBoard()
        {
            if (this._currentBoard == this._boards.Count - 1) { return; }

            this.SaveBoard();
            this.LoadBoard(this._boards.Count - 1);
        }

        public void GetNextBoard()
        {
            if (this._currentBoard >= this._boards.Count - 1){ return; }

            this.SaveBoard();
            this.LoadBoard(this._currentBoard + 1);
        }

        public void GetPrevBoard()
        {
            if (this._currentBoard == 0) { return; }

            this.SaveBoard();
            this.LoadBoard(this._currentBoard - 1);
        }
    }

    class BoardData
    {
        public string BoardXaml { get; set; }       //Xaml разметка доски
        public List<string> States { get; set; }    //Состояния доски (Undo/Redo)
        public int CurrentState { get; set; }       //Текущее состояние

        public BoardData()
        {
            this.BoardXaml = "";
            this.States = new List<string>();
            this.CurrentState = 0;
        }

        public BoardData(string state)
        {
            this.BoardXaml = "";
            this.States = new List<string>() { state };
            this.CurrentState = 0;
        }
    }
}