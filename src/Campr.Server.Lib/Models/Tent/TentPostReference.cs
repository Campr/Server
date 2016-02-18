namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostIdentifier : ModelBase
    {
        public string UserId { get; set; }
        
        public string PostId { get; set; }
        
        public string VersionId { get; set; }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return ($"{this.UserId}-{this.PostId}-{this.VersionId}").GetHashCode();
        }
    }
}