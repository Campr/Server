using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Services;

namespace Campr.Server.Lib.Helpers
{
    class TextHelpers : ITextHelpers
    {
        public TextHelpers(ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));
            this.loggingService = loggingService;
        }

        private readonly ILoggingService loggingService;
        
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
                return src;

            // If it ends with "Id", remove it.
            if (src != "Id" && src.EndsWith("Id"))
                src = src.Substring(0, src.Length - 2);

            // Insert an underscore before all uppercase chars.
            src = string.Concat(src.Select((c, i) => 
                i > 0 && char.IsUpper(c) ? "_" + c.ToString() : c.ToString()));

            // Convert to lowercase and return.
            return src.ToLowerInvariant();
        }

        public bool IsEmail(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
                return false;

            try
            {
                // Use IdnMapping class to convert Unicode domain names.
                var idn = new IdnMapping();
                src = Regex.Replace(
                    src, 
                    @"(@)(.+)$", 
                    match => match.Groups[1].Value + idn.GetAscii(match.Groups[2].Value), 
                    RegexOptions.None, 
                    TimeSpan.FromMilliseconds(200));

                // Validate email address using Regex.
                return Regex.IsMatch(
                    src,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, 
                    TimeSpan.FromMilliseconds(250));
            }
            catch (Exception ex)
            {
                this.loggingService.Exception(ex, "Error while validation email address:", src);
                return false;
            }
        }
    }
}