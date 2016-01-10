using System.Threading.Tasks;

namespace Campr.Server.Lib.Data
{
    public interface ITentBlobs
    {
        Task Initialize();
        IBlobContainer Attachments { get; }
    }
}