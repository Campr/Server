using System.Collections.Generic;
using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface ITentPostFactoryBuilder<T> where T : ModelBase
    {
        ITentPostFactoryBuilder<T> WithMentions(IList<TentMention> mentions);
        ITentPostFactoryBuilder<T> WithPostRefs(IList<TentPostRef> postRefs);
        ITentPostFactoryBuilder<T> WithAttachments(IList<TentPostAttachment> attachments);
        ITentPostFactoryBuilder<T> WithPublic(bool isPublic);
        TentPost<T> Post();
    }
}