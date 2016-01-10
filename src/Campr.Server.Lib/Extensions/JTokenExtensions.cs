using Newtonsoft.Json.Linq;
using System.Linq;

namespace Campr.Server.Lib.Extensions
{
    public static class JTokenExtensions
    {
        public static T Sort<T>(this T src) where T : JToken
        {
            // If this token is a JObject, sort it.
            var srcObject = src as JObject;
            if (srcObject != null)
            {
                var props = srcObject.Properties().ToList();

                // Remove all the properties from the object.
                props.ForEach(p => p.Remove());

                // Add them back, and sort the jObject among them.
                foreach (var prop in props.OrderBy(p => p.Name))
                {
                    // Add the property back.
                    srcObject.Add(prop);
                }
            }
            
            // Call this method reccursively on all the children of the current Token.
            foreach (var child in src.Children())
            {
                child.Sort();
            }

            // Return the resulting sorted object.
            return src;
        }
    }
}
