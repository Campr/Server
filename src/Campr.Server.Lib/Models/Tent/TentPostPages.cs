using Campr.Server.Lib.Json;

namespace Campr.Server.Lib.Models.Tent
{
    public class TentPostPages : ModelBase
    {
        // Encrypt the URLs before returning an external request.
        public void EncryptUrls()
        {
            if (!string.IsNullOrEmpty(this.First))
            {
                this.First = this.EncryptQueryString(this.First);
            }

            if (!string.IsNullOrEmpty(this.Previous))
            {
                this.Previous = this.EncryptQueryString(this.Previous);
            }

            if (!string.IsNullOrEmpty(this.Next))
            {
                this.Next = this.EncryptQueryString(this.Next);
            }

            if (!string.IsNullOrEmpty(this.Last))
            {
                this.Last = this.EncryptQueryString(this.Last);
            }
        }

        private string EncryptQueryString(string src)
        {
            //var key = RoleEnvironment.GetConfigurationSettingValue("encryptKey");
            //return "?query=" + UriHelpers.UrlEncode(CryptoHelpers.EncryptString(src, key));
            // TODO: Implement pages.
            return string.Empty;
        }

        [WebProperty]
        public string First { get; set; }

        [WebProperty]
        public string Previous { get; set; }

        [WebProperty]
        public string Next { get; set; }

        [WebProperty]
        public string Last { get; set; }
    }
}