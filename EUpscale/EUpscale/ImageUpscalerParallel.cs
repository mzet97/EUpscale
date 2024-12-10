using System.Drawing;
using System.Drawing.Imaging;

namespace EUpscale;

public class ImageUpscalerParallel
{
    public static void UpscaleImageNewtonOptimizedJpg(string inputImagePath, string outputImagePath, double scaleFactor)
    {
        Bitmap originalImage = new Bitmap(inputImagePath);

        Bitmap upscaledImage;
        if (originalImage.PixelFormat == PixelFormat.Format24bppRgb)
        {
            Bitmap redChannel = ExtractChannel(originalImage, 0);
            Bitmap greenChannel = ExtractChannel(originalImage, 1);
            Bitmap blueChannel = ExtractChannel(originalImage, 2);

            Bitmap upscaledRed = null;
            Bitmap upscaledGreen = null;
            Bitmap upscaledBlue = null;

            Parallel.Invoke(
                () => { upscaledRed = ProcessChannel(redChannel, scaleFactor); },
                () => { upscaledGreen = ProcessChannel(greenChannel, scaleFactor); },
                () => { upscaledBlue = ProcessChannel(blueChannel, scaleFactor); }
            );

            upscaledImage = CombineChannels(upscaledRed, upscaledGreen, upscaledBlue);
        }
        else
        {
            upscaledImage = ProcessChannel(originalImage, scaleFactor);
        }

        upscaledImage.Save(outputImagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        upscaledImage.Dispose();
        originalImage.Dispose();
    }

    private static Bitmap ExtractChannel(Bitmap originalImage, int channelIndex)
    {
        Bitmap channelImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < originalImage.Height; y++)
        {
            for (int x = 0; x < originalImage.Width; x++)
            {
                System.Drawing.Color originalColor = originalImage.GetPixel(x, y);
                int value = channelIndex switch
                {
                    0 => originalColor.R,
                    1 => originalColor.G,
                    2 => originalColor.B,
                    _ => 0
                };
                channelImage.SetPixel(x, y, System.Drawing.Color.FromArgb(255, value, value, value));
            }
        }
        return channelImage;
    }

    private static Bitmap ProcessChannel(Bitmap channel, double scaleFactor)
    {
        int newRows = (int)Math.Round(channel.Height * scaleFactor);
        int newCols = (int)Math.Round(channel.Width * scaleFactor);

        Bitmap upscaledChannel = new Bitmap(newCols, newRows, PixelFormat.Format32bppArgb);
        double[] xi = Linspace(0, channel.Height - 1, newRows);
        double[] yi = Linspace(0, channel.Width - 1, newCols);

        BitmapData channelData = channel.LockBits(new Rectangle(0, 0, channel.Width, channel.Height), ImageLockMode.ReadOnly, channel.PixelFormat);
        BitmapData upscaledData = upscaledChannel.LockBits(new Rectangle(0, 0, newCols, newRows), ImageLockMode.WriteOnly, upscaledChannel.PixelFormat);

        int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(channel.PixelFormat) / 8;
        byte[] channelBytes = new byte[channelData.Stride * channelData.Height];
        byte[] upscaledBytes = new byte[upscaledData.Stride * upscaledData.Height];

        System.Runtime.InteropServices.Marshal.Copy(channelData.Scan0, channelBytes, 0, channelBytes.Length);

        for (int i = 0; i < newRows; i++)
        {
            for (int j = 0; j < newCols; j++)
            {
                int originalX = (int)Math.Round(xi[i]);
                int originalY = (int)Math.Round(yi[j]);

                originalX = Math.Clamp(originalX, 0, channel.Height - 1);
                originalY = Math.Clamp(originalY, 0, channel.Width - 1);

                int originalIndex = (originalX * channelData.Stride) + (originalY * bytesPerPixel);
                int upscaledIndex = (i * upscaledData.Stride) + (j * bytesPerPixel);

                upscaledBytes[upscaledIndex] = channelBytes[originalIndex];
                upscaledBytes[upscaledIndex + 1] = channelBytes[originalIndex + 1];
                upscaledBytes[upscaledIndex + 2] = channelBytes[originalIndex + 2];
                upscaledBytes[upscaledIndex + 3] = 255;
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(upscaledBytes, 0, upscaledData.Scan0, upscaledBytes.Length);

        channel.UnlockBits(channelData);
        upscaledChannel.UnlockBits(upscaledData);

        return upscaledChannel;
    }

    private static double[] Linspace(double start, double end, int num)
    {
        double[] result = new double[num];
        double step = (end - start) / (num - 1);
        for (int i = 0; i < num; i++)
        {
            result[i] = start + i * step;
        }
        return result;
    }

    private static Bitmap CombineChannels(Bitmap redChannel, Bitmap greenChannel, Bitmap blueChannel)
    {
        Bitmap combinedImage = new Bitmap(redChannel.Width, redChannel.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < combinedImage.Height; y++)
        {
            for (int x = 0; x < combinedImage.Width; x++)
            {
                int red = redChannel.GetPixel(x, y).R;
                int green = greenChannel.GetPixel(x, y).R;
                int blue = blueChannel.GetPixel(x, y).R;
                combinedImage.SetPixel(x, y, System.Drawing.Color.FromArgb(255, red, green, blue));
            }
        }
        return combinedImage;
    }
}