using System;
using Campr.Server.Lib.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Campr.Server.Lib.Json
{
    /// <summary>
    ///     Useful when serializing/deserializing json for use with the Stack Overflow API, which produces and consumes Unix Timestamp dates
    /// </summary>
    /// <remarks>
    ///     Swiped from lfoust and fixed for latest json.net with some tweaks for handling out-of-range dates
    /// </remarks>
    public class UnixDateTimeConverter : DateTimeConverterBase
    {
        /// <summary>
        ///     Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dateValue = value as DateTime?;
            if (dateValue == null)
                throw new Exception("Expected date object value.");

            writer.WriteValue(dateValue.Value.ToUnixTime());
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
            var ticks = reader.ReadAsDouble();
            if (!ticks.HasValue)
                throw new Exception("Wrong Token Type");

            return ((long)ticks.Value).FromUnixTime();
        }
    }
}