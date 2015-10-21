using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardEditor
{
    /// <summary>
    /// Информация о клиенте
    /// </summary>
    class ClientInfo
    {
        public bool IsUpdated { get; set; }        //Получил ли клиент обновление
        public DateTime LastTime { get; set; }     //Время последнего запроса

        public ClientInfo()
        {
            this.IsUpdated = false;
            this.LastTime = DateTime.MinValue;
        }

        public ClientInfo(bool upd, DateTime time)
        {
            this.IsUpdated = upd;
            this.LastTime = time;
        }
    }
}
