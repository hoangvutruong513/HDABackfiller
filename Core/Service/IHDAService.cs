using System.Threading.Tasks;

namespace Core.Service
{
    public interface IHDAService
    {
        Task Start();
        void Stop();
    }
}
