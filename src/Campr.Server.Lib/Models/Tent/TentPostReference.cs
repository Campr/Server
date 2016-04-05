namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostIdentifier : ModelBase, ITentPostIdentifier
    {
        public TentPostIdentifier(
            string userId, 
            string postId, 
            string versionId)
        {
            this.UserId = userId;
            this.PostId = postId;
            this.VersionId = versionId;
        }

        public string UserId { get; }
        public string PostId { get; }
        public string VersionId { get; }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return $"{this.UserId}-{this.PostId}-{this.VersionId}".GetHashCode();
        }
    }
}