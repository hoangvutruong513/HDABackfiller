using OSIsoft.AF.PI;

namespace Core.ConnectionManager
{
    public interface IPIConnectionManager
    {
        (bool, PIServer) Connect();
        bool Disconnect();
    }
}
