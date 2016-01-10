using System;
using Campr.Server.Lib.Helpers;

namespace Campr.Server.Lib.Json
{
    class WebContractResolver : BaseContractResolver, IWebContractResolver
    {
        public WebContractResolver(ITextHelpers textHelpers)
            : base(textHelpers)
        {
        }

        protected override Type ToIncludeAttributeType => typeof(WebPropertyAttribute);
    }
}