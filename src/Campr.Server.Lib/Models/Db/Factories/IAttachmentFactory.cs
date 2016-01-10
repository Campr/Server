namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface IAttachmentFactory
    {
        Attachment CreateAttachment(string digest, long size, string contentType = null);
    }
}