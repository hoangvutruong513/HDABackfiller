using System.Threading.Tasks;

namespace Core.Backfiller
{
    public interface IHistoryBackfiller
    {
        Task automateBackfill();
        void logErrors();
    }
}
