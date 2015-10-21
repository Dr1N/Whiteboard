using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BoardEditor
{
    [ServiceContract]
    interface IBoardService
    {
        [OperationContract]
        byte[] GetBoard();
    }
}