using System.Threading.Tasks;

namespace Synapse3.UserInteractive
{
    public interface ISendLastInputInfo
    {
        Task SetLastInputInfo(uint time);
    }
}
