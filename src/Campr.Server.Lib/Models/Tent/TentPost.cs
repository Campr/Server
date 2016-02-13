using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Campr.Server.Lib.Extensions;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Json;
using Campr.Server.Lib.Models.Tent.PostContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable StaticMemberInGenericType
namespace Campr.Server.Lib.Models.Tent
{
    public class TentPost<TContent> : TentPost where TContent : class
    {
        private JToken jsonContent;
        private TContent content;
        public T ReadContentAs<T>() where T : class
        {
            var existingContent = this.content as T;
            if (existingContent != null)
            {
                return existingContent;
            }

            return this.jsonContent?.ToObject<T>();
        }

        public void SetContent(object newContent)
        {
            this.content = (TContent)newContent;
        }

        public override JObject GetCanonicalJson(IJsonHelpers jsonHelpers)
        {
            var result = new JObject
            {
                {"id", this.Id},
                {"entity", string.IsNullOrEmpty(this.OriginalEntity) ? this.Entity : this.OriginalEntity},
                {"type", this.Type},
                {"published_at", this.PublishedAt.GetValueOrDefault().ToUnixTime()}
            };

            // Add the necessary properties.

            // Mentions.
            if (this.Mentions != null && this.Mentions.Any())
            {
                result.Add("mentions", jsonHelpers.FromObject(this.Mentions
                    .Where(m => m.Public.GetValueOrDefault(true))
                    .Select(m => m.GetCanonicalJson(jsonHelpers))));
            }

            // Refs.
            if (this.Refs != null && this.Refs.Any())
            {
                result.Add("refs", jsonHelpers.FromObject(this.Refs.Select(p => p.GetCanonicalJson(jsonHelpers))));
            }

            // Version.
            if (this.Version != null)
            {
                result.Add("version", this.Version.GetCanonicalJson(jsonHelpers));
            }

            // Content.
            if (this.jsonContent != null && this.jsonContent.HasValues)
            {
                result.Add("content", this.jsonContent);
            }
            else if (this.content != null)
            {
                var contentObject = jsonHelpers.FromObject(this.content);
                if (contentObject.HasValues)
                {
                    result.Add("content", contentObject);
                }
            }

            // Attachments.
            if (this.Attachments != null && this.Attachments.Any())
            {
                result.Add("attachments", jsonHelpers.FromObject(this.Attachments
                    .Select(a => a.GetCanonicalJson(jsonHelpers))));
            }

            // App.
            if (this.App != null)
            {
                result.Add("app", this.App.GetCanonicalJson(jsonHelpers));
            }

            // Sort all the keys reccursively and return.
            return result.Sort();
        }

        public void Clean()
        {
            // Content.
            if (this.jsonContent != null && !this.jsonContent.HasValues)
            {
                this.jsonContent = null;
            }

            // Attachments.
            if (this.Attachments != null && !this.Attachments.Any())
            {
                this.Attachments = null;
            }
        }

        public void ResponseClean(bool removeReceiveDate, bool removeDefaultPermissions)
        {
            // Received date.
            if (removeReceiveDate)
            {
                this.ReceivedAt = null;
                this.Version.ReceivedAt = null;
            }

            // Permissions.
            if (removeDefaultPermissions && this.Permissions.IsDefault())
            {
                this.Permissions = null;
            }
        }

        [DbProperty]
        [WebProperty]
        [JsonConverter(typeof(TentPostContentConverter))]
        public TContent Content { get; set; }
    }

    public class TentPost : ModelBase
    {
        public bool Validate()
        {
            return this.Mentions?.All(m => m.Validate()) ?? true;
        }

        // Core properties.
        [DbProperty]
        [WebProperty]
        public string Id { get; set; }

        [DbProperty]
        public string UserId { get; set; }

        [DbProperty]
        [WebProperty]
        public string Entity { get; set; }

        [DbProperty]
        [WebProperty]
        public string OriginalEntity { get; set; }

        [DbProperty]
        [WebProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? PublishedAt { get; set; }

        [DbProperty]
        [WebProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? ReceivedAt { get; set; }

        [DbProperty]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? DeletedAt { get; set; }

        [DbProperty]
        [WebProperty]
        public string Type { get; set; }
        
        [DbProperty]
        [WebProperty]
        public TentApp App { get; set; }
        
        [DbProperty]
        [WebProperty]
        public TentVersion Version { get; set; }
        
        [DbProperty]
        [WebProperty]
        public List<TentMention> Mentions { get; set; }
        
        [DbProperty]
        [WebProperty]
        public List<TentPostRef> Refs { get; set; }
        
        [DbProperty]
        [WebProperty]
        public List<TentPostAttachment> Attachments { get; set; }
        
        [DbProperty]
        [WebProperty]
        public List<TentLicense> Licenses { get; set; }
        
        [DbProperty]
        [WebProperty]
        public TentPermissions Permissions { get; set; }

        #region Transient properties.

        public List<HttpContent> NewAttachments { get; set; }
        public TentPost<TentContentCredentials> PassengerCredentials { get; set; }

        #endregion
    }
}