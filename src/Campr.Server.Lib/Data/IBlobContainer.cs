namespace Campr.Server.Lib.Data
{
    public interface IBlobContainer
    {
        IBlob GetBlob(string blobId);
    }
}