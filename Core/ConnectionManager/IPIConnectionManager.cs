using OSIsoft.AF.PI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ConnectionManager
{
    public interface IPIConnectionManager
    {
        (bool, PIServer) Connect();
        bool Disconnect();
    }
}
