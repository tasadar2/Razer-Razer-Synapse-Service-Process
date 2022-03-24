using System.Threading.Tasks;

namespace Synapse3.UserInteractive
{
    public interface IRegistryChangedInfo
    {
        Task SetRegistryChangedInfo(string info);
    }
}
