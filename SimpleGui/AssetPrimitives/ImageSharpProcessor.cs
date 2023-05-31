using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetPrimitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Veldrid;

namespace AssetProcessor
{
    internal class ImageSharpProcessor : BinaryAssetProcessor<ProcessedTexture>
    {
        public override ProcessedTexture ProcessT(Stream stream, string extension)
        {
            
            // Load the image.
            Image<Rgba32> image = Image.Load<Rgba32>(stream);
            
            // Create all the image Mipmaps
            Image<Rgba32>[] mipmaps = GenerateMipmaps(image, out int totalSize);

            // Allocate the working memory.
            byte[] allTexData = new byte[totalSize * Unsafe.SizeOf<Rgba32>()];
            
            // Take the working memory and create a Rgba32 Span with it, makes the math a little easier to work with, less SizeOf calculations.
            Span<Rgba32> textureData = MemoryMarshal.Cast<byte, Rgba32>(allTexData);

            // Initialize the offset to Zero
            int offset = 0;
            
            // For each mipmap
            foreach (Image<Rgba32> mipmap in mipmaps)
            {
                // Calculate the amount of Rgba32 pixels in said mipmap
                int mipPixels = mipmap.Width * mipmap.Height;
                
                // Slice the full memory for the Rgba32 mipmap texture into it's own span
                Span<Rgba32> mipTexture  = textureData.Slice(offset, mipPixels);

                // ImageSharp does not guarantee rows are stored Contiguously in memory, so this needs to be done per row.
                for (int rowIndex = 0; rowIndex < mipmap.Height; rowIndex++)
                {
                    // Copy the pixels in the Row mipmap to the corresponding row in the texture
                    mipmap.DangerousGetPixelRowMemory(rowIndex).Span.CopyTo(mipTexture.Slice(mipmap.Width * rowIndex, mipmap.Width));
                }
                
                // Add the offset for the next loop.
                offset += mipPixels;
            }
            
            // Create the texture and return it.
            ProcessedTexture texData = new ProcessedTexture(
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureType.Texture2D,
                    (uint)image.Width, (uint)image.Height, 1,
                    (uint)mipmaps.Length, 1,
                    allTexData);
            return texData;
        }

        // Taken from Veldrid.ImageSharp

        private static readonly IResampler s_resampler = new LanczosResampler();

        private static Image<T>[] GenerateMipmaps<T>(Image<T> baseImage, out int totalSize) where T : unmanaged, IPixel<T>
        {
            int mipLevelCount = ComputeMipLevels(baseImage.Width, baseImage.Height);
            Image<T>[] mipLevels = new Image<T>[mipLevelCount];
            mipLevels[0] = baseImage;
            totalSize = baseImage.Width * baseImage.Height * Unsafe.SizeOf<T>();
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while (currentWidth != 1 || currentHeight != 1)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<T> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, s_resampler));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                totalSize += newWidth * newHeight;
                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }

        public static int ComputeMipLevels(int width, int height)
        {
            return 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }
    }
}
