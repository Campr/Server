using System;
using System.Linq;
using System.Reflection;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Tent;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Campr.Server.Lib.Json
{
    class BaseContractResolver : DefaultContractResolver, IBaseContractResolver
    {
        #region Constructor & Private variables.

        protected BaseContractResolver(ITextHelpers textHelpers, Type toIncludeAttributeType)
        {
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            Ensure.Argument.IsNotNull(toIncludeAttributeType, nameof(toIncludeAttributeType));

            this.textHelpers = textHelpers;
            this.toIncludeAttributeType = toIncludeAttributeType;
            this.modelType = typeof(ModelBase);
        }

        private readonly ITextHelpers textHelpers;
        private readonly Type toIncludeAttributeType;
        private readonly Type modelType;

        #endregion

        #region Base class override.

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            // Check if this property needs to be serialized.
            if (this.toIncludeAttributeType != null && member.DeclaringType != null && this.modelType.IsAssignableFrom(member.DeclaringType)
                && !member.GetCustomAttributes(this.toIncludeAttributeType, false).Any())
            {
                return null;
            }

            // Use the base implementation to create the property.
            return base.CreateProperty(member, memberSerialization); ;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            // Rewrite the property name to use camel case and remove the Id.
            return this.textHelpers.ToJsonPropertyName(propertyName);
        }

        #endregion
    }
}