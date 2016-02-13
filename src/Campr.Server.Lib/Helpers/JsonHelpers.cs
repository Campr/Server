using System;
using System.Globalization;
using System.IO;
using System.Text;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Helpers
{
    class JsonHelpers : IWebJsonHelpers, IDbJsonHelpers
    {
        public JsonHelpers(IBaseContractResolver contractResolver)
        {
            Ensure.Argument.IsNotNull(contractResolver, nameof(contractResolver));
            
            // Create our serializer.
            this.serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                ContractResolver = contractResolver
            });
        }
        
        private readonly JsonSerializer serializer;

        #region IJsonHelpers implementation.

        public string ToJsonString(object obj)
        {
            if (obj == null)
                return null;

            var sb = new StringBuilder(256);
            var sw = new StringWriter(sb, CultureInfo.InvariantCulture);

            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = this.serializer.Formatting;
                this.serializer.Serialize(jsonWriter, obj);
            }

            return sw.ToString();
        }

        public string ToJsonStringUnescaped(object obj)
        {
            // Generate the base JSON string.
            var jsonString = this.ToJsonString(obj);

            // Unescape it.
            // TODO: Replace this with Json.Net modification.
            jsonString = jsonString.Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\r", "\r")
                .Replace("\\f", "\f")
                .Replace("\\b", "\b")
                .Replace("\\u0085", "\u0085")
                .Replace("\\u2028", "\u2028")
                .Replace("\\u2029", "\u2029");

            return jsonString;
        }

        public T FromJsonString<T>(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
                return default(T);
            

            using (var jsonReader = new JsonTextReader(new StringReader(src)))
            {
                return this.serializer.Deserialize<T>(jsonReader);
            }
        }

        public T TryFromJsonString<T>(string src)
        {
            try
            {
                return this.FromJsonString<T>(src);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public JToken FromObject(object src)
        {
            return JToken.FromObject(src, this.serializer);
        }

        #endregion
    }
}
