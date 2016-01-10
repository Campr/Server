using System.Threading.Tasks;

namespace Campr.Server.Lib.Logic
{
    public interface IAttachmentLogic
    {
        Task<string> SaveAttachment(byte[] data, string digest = null, string contentType = null);
    }
}