using System.Threading.Tasks;
using Campr.Server.Lib.Models.Db;

namespace Campr.Server.Lib.Repositories
{
    public interface IAttachmentRepository
    {
        Task<Attachment> GetAttachmentAsync(string digest);
        Task UpdateAttachmentAsync(Attachment attachment);
    }
}