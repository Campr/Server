using System;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Logic
{
    public interface IBewitLogic
    {
        Task<string> CreateBewitForPostAsync(string userHandle, string postId, TimeSpan? expiresIn = null);
    }
}