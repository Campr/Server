using System;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Other;
using Campr.Server.Lib.Models.Other.Factories;
using Newtonsoft.Json;

namespace Campr.Server.Lib.Json
{
    public class TentPostTypeConverter : JsonConverter
    {
        public TentPostTypeConverter(ITentPostTypeFactory postTypeFactory)
        {
            Ensure.Argument.IsNotNull(postTypeFactory, nameof(postTypeFactory));
            this.postTypeFactory = postTypeFactory;
        }

        private readonly ITentPostTypeFactory postTypeFactory;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var postType = value as ITentPostType;
            writer.WriteValue(postType?.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Read and validate the value.
            var stringValue = reader.ReadAsString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            // Parse it as a Post Type.
            return this.postTypeFactory.FromString(stringValue);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TentPostType) || objectType == typeof(ITentPostType);
        }
    }
}