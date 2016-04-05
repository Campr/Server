namespace Campr.Server.Lib.Models.Tent
{
    public interface ITentPostIdentifier
    {
        string UserId { get; }
        string PostId { get; }
        string VersionId { get; }
    }
}