namespace Campr.Server.Lib.Helpers
{
    public interface ITextHelpers
    {
        string GenerateUniqueId();
        string CapitalizeFirstLetter(string src);
        string ToJsonPropertyName(string src);
        bool IsEmail(string src);
    }
}