using Campr.Server.Lib.Models.Tent;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface ITentPostFactoryBuilder<T> where T : class
    {
        ITentPostFactoryBuilder<T> WithMentions(params TentMention[] mentions);
        ITentPostFactoryBuilder<T> WithPostRefs(params TentPostRef[] postRefs);
        ITentPostFactoryBuilder<T> WithAttachments(params TentPostAttachment[] attachments);
        ITentPostFactoryBuilder<T> WithPublic(bool isPublic);
        TentPost<T> Post();
    }
}