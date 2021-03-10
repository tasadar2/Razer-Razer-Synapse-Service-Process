using System.Threading.Tasks;

namespace Synapse3.UserInteractive
{
    public interface ISetForegroundWindow
    {
        Task SetForegroundWindow(string exe);
    }
}
