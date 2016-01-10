using SharpDX.Mathematics.Interop;

namespace Campr.Server.Lib.Images
{
    public interface IImageEngine
    {
        RawColor4 GenerateDefaultColor(string seed, double value = 0.8);
        string ColorToHex(RawColor4 color);
        byte[] GenerateDefaultAvatar(string seed, int size);
        byte[] GenerateDefaultProfileHeader(string seed);
        byte[] ResizeImage(byte[] image, int targetWidth, int targetHeight);
    }
}