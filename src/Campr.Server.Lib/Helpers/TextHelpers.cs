using System;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Campr.Server.Lib.Helpers
{
    class TextHelpers : ITextHelpers
    {
        public string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string CapitalizeFirstLetter(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return src;
            }

            return src.Substring(0, 1).ToUpper() + src.Substring(1, src.Length - 1).ToLower();
        }

        public string ToJsonPropertyName(string src)
        {
            // Make sure we have something to work with.
            if (string.IsNullOrWhiteSpace(src))
            {
                return src;
            }

            // If it ends with "Id", remove it.
            if (src.Length > 2 && src.EndsWith("Id"))
            {
                src = src.Substring(0, src.Length - 2);
            }

            // Insert an underscore before all uppercase chars.
            src = string.Concat(src.Select((c, i) => 
                i > 0 && char.IsUpper(c) ? "_" + c.ToString() : c.ToString()));

            // Convert to lowercase and return.
            return src.ToLowerInvariant();
        }

        public bool IsEmail(string src)
        {
            try
            {
                src = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(src));
                var targetAddress = new MailAddress(src);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}