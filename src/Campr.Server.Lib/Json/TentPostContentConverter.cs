using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Campr.Server.Lib.Json
{
    public class TentPostContentConverter : JsonConverter
    {
        /// <summary>
        ///     Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // If it's a JSON string, write it directly.
            var stringValue = value as string;
            if (stringValue != null)
            {
                writer.WriteRawValue(stringValue);
                return;
            }

            // If it's a JToken, we can also write it directly.
            var jTokenValue = value as JToken;
            if (jTokenValue != null)
            {
                jTokenValue.WriteTo(writer);
                return;
            }

            // Otherwise, serialize it.
            serializer.Serialize(writer, value);
        }

        /// <summary>
        ///     Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref = "JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JToken.ReadFrom(reader);
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}