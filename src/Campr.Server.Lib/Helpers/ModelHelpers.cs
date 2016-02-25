using Campr.Server.Lib.Exceptions;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Models.Db;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Helpers
{
    class ModelHelpers : IModelHelpers
    {
        public ModelHelpers(
            IUriHelpers uriHelpers, 
            IJsonHelpers jsonHelpers, 
            ICryptoHelpers cryptoHelpers)
        {
            Ensure.Argument.IsNotNull(uriHelpers, nameof(uriHelpers));
            Ensure.Argument.IsNotNull(jsonHelpers, nameof(jsonHelpers));
            Ensure.Argument.IsNotNull(cryptoHelpers, nameof(cryptoHelpers));

            this.uriHelpers = uriHelpers;
            this.jsonHelpers = jsonHelpers;
            this.cryptoHelpers = cryptoHelpers;
        }

        private readonly IUriHelpers uriHelpers;
        private readonly IJsonHelpers jsonHelpers;
        private readonly ICryptoHelpers cryptoHelpers;

        private const int ShortVersionLength = 20;

        public string GetUserEntity(User user)
        {
            Ensure.Argument.IsNotNull(user, nameof(user));

            return user.IsInternal()
                ? this.uriHelpers.GetCamprTentEntity(user.Handle)
                : user.Entity;
        }

        public string GetVersionIdFromPost<T>(TentPost<T> post) where T : class
        {
            Ensure.Argument.IsNotNull(post, nameof(post));

            // Generate the JSON string.
            var canonicalJson = post.GetCanonicalJson(this.jsonHelpers);
            var stringJson = this.jsonHelpers.ToJsonStringUnescaped(canonicalJson);

            // Hash and truncate the resulting JSON.
            var computedVersionId = this.cryptoHelpers.ConvertToSha512TruncatedWithPrefix(stringJson);

            if (!string.IsNullOrEmpty(post.Version?.Id) && post.Version.Id != computedVersionId)
                throw new VersionMismatchException(post.Version.Id, computedVersionId);
            
            return computedVersionId;
        }

        public string GetShortVersionId(string versionId)
        {
            return versionId.Length > ShortVersionLength
                ? versionId.Substring(versionId.Length - ShortVersionLength, ShortVersionLength)
                : versionId;
        }
    }
}