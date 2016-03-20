namespace Campr.Server.Lib.Models.Other
{
    public class TentPostType : ITentPostType
    {
        public TentPostType(
            string type,
            string subType,
            bool wildcard)
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

        public override bool Equals(object obj)
        {
            return this.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion
    }
}
