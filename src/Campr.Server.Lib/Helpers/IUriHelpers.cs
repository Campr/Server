using System;

namespace Campr.Server.Lib.Helpers
{
    public interface IUriHelpers
    {
        string UrlEncode(string src);
        string UrlDecode(string url);
        string UrlTokenEncode(byte[] src);
        byte[] UrlTokenDecode(string url);
        Uri RemoveUriQuery(Uri src);
        bool IsCamprHandle(string handle);
        bool IsCamprEntity(string entity, out string handle);
        bool IsCamprDomain(string domain, out string handle);
        bool IsValidUri(string src);
        string ExtractUsernameFromPath(string path);
        string GetStandardEntity(string entity);
        string GetCamprTentEntity(string userHandle);
        Uri GetCamprPostUri(string userHandle, string postId);
        Uri GetCamprPostBewitUri(string userHandle, string postId, string bewit);
        Uri GetCamprAttachmentUri(string userHandle, string entity, string digest);
        string GetCamprPostPath(string userHandle, string postId);
        Uri GetCamprUriFromPath(string path);
        Uri GetCamprUriFromUri(Uri uri);
    }
}