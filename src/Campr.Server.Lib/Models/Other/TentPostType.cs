namespace Campr.Server.Lib.Models.Other
{
    public class TentPostType : ITentPostType
    {
        public TentPostType(
            string type,
            string subType = null,
            bool wildcard = false)
        {
            this.Type = type;
            this.SubType = subType;
            this.WildCard = wildcard;
        }

        #region Public properties.

        public string Type { get; }
        public string SubType { get; }
        public bool WildCard { get; }

        #endregion

        #region Method overriding.

        public override string ToString()
        {
            return this.WildCard 
                ? this.Type
                : $"{this.Type}#{this.SubType}";
        }

        public bool Equals(ITentPostType other)
        {
            return this.ToString() == other.ToString();
        }

        public override bool Equals(object other)
        {
            return this.Equals(other as ITentPostType);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion
    }
}
