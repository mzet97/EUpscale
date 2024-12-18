using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

public class ImageUpscalerParallel
{
    public static void UpscaleImageNewtonOptimizedJpg(string inputImagePath, string outputImagePath, double scaleFactor)
    {
        using (Image<Rgba32> originalImage = Image.Load<Rgba32>(inputImagePath))
        {
            Image<Rgba32> upscaledImage;

            if (originalImage.PixelType.BitsPerPixel == 32)
            {
                var (redChannel, greenChannel, blueChannel) = ExtractChannels(originalImage);

                Image<Rgba32> upscaledRed = null;
                Image<Rgba32> upscaledGreen = null;
                Image<Rgba32> upscaledBlue = null;

                System.Threading.Tasks.Parallel.Invoke(
                    () => { upscaledRed = ProcessChannel(redChannel, scaleFactor); },
                    () => { upscaledGreen = ProcessChannel(greenChannel, scaleFactor); },
                    () => { upscaledBlue = ProcessChannel(blueChannel, scaleFactor); }
                );

                upscaledImage = CombineChannels(upscaledRed, upscaledGreen, upscaledBlue);

                upscaledRed.Dispose();
                upscaledGreen.Dispose();
                upscaledBlue.Dispose();
            }
            else
            {
                upscaledImage = ProcessChannel(originalImage, scaleFactor);
            }

            upscaledImage.Mutate(x => x.GaussianBlur(1.5f));

            upscaledImage.SaveAsJpeg(outputImagePath);
            upscaledImage.Dispose();
        }
    }

    private static (Image<Rgba32>, Image<Rgba32>, Image<Rgba32>) ExtractChannels(Image<Rgba32> originalImage)
    {
        var redChannel = new Image<Rgba32>(originalImage.Width, originalImage.Height);
        var greenChannel = new Image<Rgba32>(originalImage.Width, originalImage.Height);
        var blueChannel = new Image<Rgba32>(originalImage.Width, originalImage.Height);

        for (int y = 0; y < originalImage.Height; y++)
        {
            for (int x = 0; x < originalImage.Width; x++)
            {
                var pixel = originalImage[x, y];
                redChannel[x, y] = new Rgba32(pixel.R, pixel.R, pixel.R, 255);
                greenChannel[x, y] = new Rgba32(pixel.G, pixel.G, pixel.G, 255);
                blueChannel[x, y] = new Rgba32(pixel.B, pixel.B, pixel.B, 255);
            }
        }

        return (redChannel, greenChannel, blueChannel);
    }

    private static Image<Rgba32> ProcessChannel(Image<Rgba32> channel, double scaleFactor)
    {
        int newWidth = (int)Math.Round(channel.Width * scaleFactor);
        int newHeight = (int)Math.Round(channel.Height * scaleFactor);

        var upscaledChannel = channel.Clone(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Bicubic));
        return upscaledChannel;
    }

    private static Image<Rgba32> CombineChannels(Image<Rgba32> redChannel, Image<Rgba32> greenChannel, Image<Rgba32> blueChannel)
    {
        var combinedImage = new Image<Rgba32>(redChannel.Width, redChannel.Height);
        for (int y = 0; y < combinedImage.Height; y++)
        {
            for (int x = 0; x < combinedImage.Width; x++)
            {
                var r = redChannel[x, y].R;
                var g = greenChannel[x, y].R;
                var b = blueChannel[x, y].R;
                combinedImage[x, y] = new Rgba32(r, g, b, 255);
            }
        }
        return combinedImage;
    }
}
