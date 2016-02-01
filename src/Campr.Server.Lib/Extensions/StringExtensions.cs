namespace Campr.Server.Lib.Extensions
{
    public static class StringExtensions
    {
        public static int? TryParseInt(this string src)
        {
            int result;
            return int.TryParse(src, out result) ? result : (int?)null;
        }

        public static long? TryParseLong(this string src)
        {
            long result;
            return long.TryParse(src, out result) ? result : (long?)null;
        }
    }
}