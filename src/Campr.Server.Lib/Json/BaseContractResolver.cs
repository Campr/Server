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

        protected BaseContractResolver(ITextHelpers textHelpers)
        {
            Ensure.Argument.IsNotNull(textHelpers, nameof(textHelpers));
            this.textHelpers = textHelpers;
            this.modelType = typeof(ModelBase);
        }

        private readonly ITextHelpers textHelpers;
        private readonly Type modelType;

        #endregion

        #region Abstract properties.

        protected virtual Type ToIncludeAttributeType { get; }

        #endregion

        #region Base class override.

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            // Check if this property needs to be serialized.
            if (this.ToIncludeAttributeType != null && member.DeclaringType != null && this.modelType.IsAssignableFrom(member.DeclaringType)
                && !member.GetCustomAttributes(this.ToIncludeAttributeType, false).Any())
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