namespace Campr.Server.Lib.Models.Other
{
    public class TentPostType : ITentPostType
    {
        #region Public properties.

        public string Type { get; set; }
        public string SubType { get; set; }
        public bool WildCard { get; set; }

        #endregion

        #region Method overriding.

        public override string ToString()
        {
            return this.WildCard 
                ? this.Type
                : string.Format("{0}#{1}", this.Type, this.SubType);
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
