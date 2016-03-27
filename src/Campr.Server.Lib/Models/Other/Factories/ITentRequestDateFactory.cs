namespace Campr.Server.Lib.Models.Other.Factories
{
    public interface ITentRequestDateFactory
    {
        ITentRequestDate FromString(string date);
        ITentRequestDate FromPost(ITentRequestPost post);
        ITentRequestDate MinValue();
    }
}