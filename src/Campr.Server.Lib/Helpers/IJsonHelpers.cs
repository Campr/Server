using Couchbase.Core.Serialization;

namespace Campr.Server.Lib.Helpers
{
    public interface IJsonHelpers : ITypeSerializer
    {
        string ToJsonString(object obj);
        string ToJsonStringUnescaped(object obj);
        T FromJsonString<T>(string src);
        T TryFromJsonString<T>(string src);
    }
}