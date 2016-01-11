using System;
using System.IO;
using System.Linq;
using System.Text;
using Campr.Server.Lib.Configuration;
using Campr.Server.Lib.Helpers;
using Campr.Server.Lib.Infrastructure;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using WIC = SharpDX.WIC;
using Direct2D1 = SharpDX.Direct2D1;

namespace Campr.Server.Lib.Images
{
    class ImageEngine : IImageEngine
    {
        #region Constructor & Private variables.

        public ImageEngine(ICryptoHelpers cryptoHelpers,
            IGeneralConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(cryptoHelpers, "cryptoHelpers");

            this.cryptoHelpers = cryptoHelpers;
            //this.avatarOverlay = new Lazy<byte[]>(() =>
            //    File.ReadAllBytes(configuration.AvatarOverlayPath()));
            //this.profileHeaderOverlay = new Lazy<byte[]>(() =>
            //    File.ReadAllBytes(configuration.ProfileHeaderOverlayPath()));
        }

        private readonly ICryptoHelpers cryptoHelpers;
        private readonly Lazy<byte[]> avatarOverlay;
        private readonly Lazy<byte[]> profileHeaderOverlay; 

        #endregion

        #region Interface implementation.

        public RawColor4 GenerateDefaultColor(string seed, double value = 0.8)
        {
            // Hash the seed for more randomness.
            var seedHash = this.cryptoHelpers.ConvertToSha512Truncated(seed);
            var seedBytes = Encoding.UTF8.GetBytes(seedHash);
            var seedSum = seedBytes.Aggregate(0, (current, next) => current + next);

            // Derive the random color from the seed.
            var rand = seedSum % 360;
            return this.ColorFromHSV(rand, 0.5, value);
        }

        public string ColorToHex(RawColor4 color)
        {
            return '#'
                + ((int)(color.R * 255)).ToString("X2") 
                + ((int)(color.G * 255)).ToString("X2") 
                + ((int)(color.B * 255)).ToString("X2");
        }

        public byte[] GenerateDefaultAvatar(string seed, int size)
        {
            var color = this.GenerateDefaultColor(seed);
            var pixelFormat = WIC.PixelFormat.Format32bppPBGRA;

            // Create the factories.
            using (var wicFactory = new WIC.ImagingFactory())
            using (var d2DFactory = new Direct2D1.Factory(Direct2D1.FactoryType.SingleThreaded))
            {
                var renderTargetProperties = new Direct2D1.RenderTargetProperties(Direct2D1.RenderTargetType.Default, new Direct2D1.PixelFormat(Format.Unknown, Direct2D1.AlphaMode.Unknown), 96f, 96f, Direct2D1.RenderTargetUsage.None, Direct2D1.FeatureLevel.Level_DEFAULT);

                // Create the bitmap objects.
                using (var wicBitmap = new WIC.Bitmap(wicFactory, size, size, pixelFormat, WIC.BitmapCreateCacheOption.CacheOnDemand))
                using (var d2DBitmapTarget = new Direct2D1.WicRenderTarget(d2DFactory, wicBitmap, renderTargetProperties))
                {
                    // Draw on the BitmapTarget.
                    d2DBitmapTarget.AntialiasMode = Direct2D1.AntialiasMode.PerPrimitive;
                    d2DBitmapTarget.BeginDraw();

                    d2DBitmapTarget.Clear(color);

                    // Load the overlay image.
                    using (var source = this.GetBitmap(this.avatarOverlay.Value, wicFactory, new Size2(size, size)))
                    using (var overlay = Direct2D1.Bitmap.FromWicBitmap(d2DBitmapTarget, source))
                    {
                        d2DBitmapTarget.DrawBitmap(overlay, new RawRectangleF(0, 0, size, size), 1f, Direct2D1.BitmapInterpolationMode.Linear);
                    }

                    d2DBitmapTarget.EndDraw();

                    // Render the bitmap.
                    using (var outputStream = new MemoryStream())
                    using (var imageStream = new WIC.WICStream(wicFactory, outputStream))
                    using (var encoder = new WIC.BitmapEncoder(wicFactory, WIC.ContainerFormatGuids.Jpeg))
                    {
                        encoder.Initialize(imageStream);

                        var bitmapFrameEncode = new WIC.BitmapFrameEncode(encoder);
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetResolution(96, 96);
                        bitmapFrameEncode.SetSize(size, size);
                        bitmapFrameEncode.Options.ImageQuality = .85f;

                        bitmapFrameEncode.WriteSource(wicBitmap);

                        bitmapFrameEncode.Commit();
                        encoder.Commit();

                        return outputStream.GetBuffer();
                    }
                }
            }
        }

        public byte[] GenerateDefaultProfileHeader(string seed)
        {
            var color = this.GenerateDefaultColor(seed);
            var pixelFormat = WIC.PixelFormat.Format32bppPBGRA;
            const int width = 940;
            const int height = 196;

            // Create the factories.
            using (var wicFactory = new WIC.ImagingFactory())
            using (var d2DFactory = new Direct2D1.Factory(Direct2D1.FactoryType.SingleThreaded))
            {
                var renderTargetProperties = new Direct2D1.RenderTargetProperties(Direct2D1.RenderTargetType.Default, new Direct2D1.PixelFormat(Format.Unknown, Direct2D1.AlphaMode.Unknown), 96f, 96f, Direct2D1.RenderTargetUsage.None, Direct2D1.FeatureLevel.Level_DEFAULT);

                // Create the bitmap objects.
                using (var wicBitmap = new WIC.Bitmap(wicFactory, width, height, pixelFormat, WIC.BitmapCreateCacheOption.CacheOnDemand))
                using (var d2DBitmapTarget = new Direct2D1.WicRenderTarget(d2DFactory, wicBitmap, renderTargetProperties))
                {
                    // Draw on the BitmapTarget.
                    d2DBitmapTarget.AntialiasMode = Direct2D1.AntialiasMode.PerPrimitive;
                    d2DBitmapTarget.BeginDraw();

                    d2DBitmapTarget.Clear(color);

                    // Load the overlay image.
                    using (var source = this.GetBitmap(this.profileHeaderOverlay.Value, wicFactory, new Size2(width, height)))
                    using (var overlay = Direct2D1.Bitmap.FromWicBitmap(d2DBitmapTarget, source))
                    {
                        d2DBitmapTarget.DrawBitmap(overlay, new RawRectangleF(0, 0, width, height), 1f, Direct2D1.BitmapInterpolationMode.Linear);
                    }

                    d2DBitmapTarget.EndDraw();

                    // Render the bitmap.
                    using (var outputStream = new MemoryStream())
                    using (var imageStream = new WIC.WICStream(wicFactory, outputStream))
                    using (var encoder = new WIC.BitmapEncoder(wicFactory, WIC.ContainerFormatGuids.Jpeg))
                    {
                        encoder.Initialize(imageStream);

                        var bitmapFrameEncode = new WIC.BitmapFrameEncode(encoder);
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetResolution(96, 96);
                        bitmapFrameEncode.SetSize(width, height);
                        bitmapFrameEncode.Options.ImageQuality = .85f;

                        bitmapFrameEncode.WriteSource(wicBitmap);

                        bitmapFrameEncode.Commit();
                        encoder.Commit();

                        return outputStream.GetBuffer();
                    }
                }
            }
        }

        public byte[] ResizeImage(byte[] image, int targetWidth, int targetHeight)
        {
            var targetSize = new Size2(targetWidth, targetHeight);
            var pixelFormat = WIC.PixelFormat.Format32bppPBGRA;

            // Create the factories.
            using (var wicFactory = new WIC.ImagingFactory())
            using (var imageSource = this.GetBitmap(image, wicFactory, default(Size2), true))
            using (var d2DFactory = new Direct2D1.Factory(Direct2D1.FactoryType.SingleThreaded))
            {
                // Update the target size if some values weren't specified.
                var imageRatio = imageSource.Size.Width / (double)imageSource.Size.Height;

                if (targetSize.Width < 0) targetSize.Width = (int)(targetSize.Height * imageRatio);
                else if (targetSize.Height < 0) targetSize.Height = (int)(targetSize.Width * (1.0 / imageRatio));

                var renderTargetProperties = new Direct2D1.RenderTargetProperties(Direct2D1.RenderTargetType.Default, new Direct2D1.PixelFormat(Format.Unknown, Direct2D1.AlphaMode.Unknown), 96f, 96f, Direct2D1.RenderTargetUsage.None, Direct2D1.FeatureLevel.Level_DEFAULT);

                // Create the bitmap objects.
                using (var wicBitmap = new WIC.Bitmap(wicFactory, targetSize.Width, targetSize.Height, pixelFormat, WIC.BitmapCreateCacheOption.CacheOnDemand))
                using (var d2DBitmapTarget = new Direct2D1.WicRenderTarget(d2DFactory, wicBitmap, renderTargetProperties))
                {
                    // Draw on the BitmapTarget.
                    d2DBitmapTarget.AntialiasMode = Direct2D1.AntialiasMode.PerPrimitive;
                    d2DBitmapTarget.BeginDraw();

                    d2DBitmapTarget.Clear(new RawColor4(1, 1, 1, 1));

                    // Load the overlay image.
                    using (var scaler = new WIC.BitmapScaler(wicFactory))
                    {
                        var imageSize = this.GetUniformToFillSize(targetSize, imageSource.Size);

                        var top = (int)((imageSize.Height - targetSize.Height) / 2.0);
                        var left = (int)((imageSize.Width - targetSize.Width) / 2.0);
                        var imageRect = new RawRectangleF(left, top, targetSize.Width, targetSize.Height);

                        scaler.Initialize(imageSource, imageSize.Width, imageSize.Height, WIC.BitmapInterpolationMode.Fant);

                        using (var overlay = Direct2D1.Bitmap.FromWicBitmap(d2DBitmapTarget, scaler))
                        {
                            d2DBitmapTarget.DrawBitmap(overlay, new RawRectangleF(0, 0, targetSize.Width, targetSize.Height), 1f, Direct2D1.BitmapInterpolationMode.Linear, imageRect);
                        }
                    }

                    d2DBitmapTarget.EndDraw();

                    // Render the bitmap.
                    using (var outputStream = new MemoryStream())
                    using (var imageStream = new WIC.WICStream(wicFactory, outputStream))
                    using (var encoder = new WIC.BitmapEncoder(wicFactory, WIC.ContainerFormatGuids.Jpeg))
                    {
                        encoder.Initialize(imageStream);

                        var bitmapFrameEncode = new WIC.BitmapFrameEncode(encoder);
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetResolution(96, 96);
                        bitmapFrameEncode.SetSize(targetSize.Width, targetSize.Height);
                        bitmapFrameEncode.Options.ImageQuality = .85f;

                        bitmapFrameEncode.WriteSource(wicBitmap);

                        bitmapFrameEncode.Commit();
                        encoder.Commit();

                        return outputStream.GetBuffer();
                    }
                }
            }
        }

        #endregion

        #region Private methods.

        private WIC.BitmapSource GetBitmap(byte[] image, WIC.ImagingFactory factory, Size2 targetSize, bool noResize = false)
        {
            var stream = new WIC.WICStream(factory, new MemoryStream(image));
            var decoder = new WIC.BitmapDecoder(factory, stream, WIC.DecodeOptions.CacheOnDemand);

            var frame = decoder.GetFrame(0);
            WIC.BitmapSource source;

            if (!noResize &&
                (frame.Size.Width != targetSize.Width || frame.Size.Height != targetSize.Height))
            {
                var scaler = new WIC.BitmapScaler(factory);
                scaler.Initialize(frame, targetSize.Width, targetSize.Height, WIC.BitmapInterpolationMode.Fant);
                source = scaler;
            }
            else
            {
                source = frame;
            }

            var converter = new WIC.FormatConverter(factory);
            converter.Initialize(source, WIC.PixelFormat.Format32bppPBGRA, WIC.BitmapDitherType.None, null, 0.0, WIC.BitmapPaletteType.Custom);
            return converter;
        }
        
        private Size2 GetUniformToFillSize(Size2 targetSize, Size2 imageSize)
        {
            var result = new Size2();

            var targetRatio = targetSize.Width / (double)targetSize.Height;
            var imageRatio = imageSize.Width / (double)imageSize.Height;

            if (targetRatio > imageRatio)
            {
                result.Width = targetSize.Width;
                result.Height = (int)((1.0 / imageRatio) * targetSize.Width);
            }
            else
            {
                result.Width = (int)(imageRatio * targetSize.Height);
                result.Height = targetSize.Height;
            }

            return result;
        }

        private RawColor4 ColorFromHSV(double hue, double saturation, double value)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            var v = (float)value;
            var p = (float)(value * (1 - saturation));
            var q = (float)(value * (1 - f * saturation));
            var t = (float)(value * (1 - (1 - f) * saturation));

            if (hi == 0) return new RawColor4(v, t, p, 1f);
            if (hi == 1) return new RawColor4(q, v, p, 1f);
            if (hi == 2) return new RawColor4(p, v, t, 1f);
            if (hi == 3) return new RawColor4(p, q, v, 1f);
            if (hi == 4) return new RawColor4(t, p, v, 1f);
            
            return new RawColor4(v, p, q, 1f);
        } 

        #endregion
    }
}