namespace Moire
{
	using R3.Core;
	using R3.Geometry;
    using System;
    using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
    using System.IO;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
	using Math = System.Math;

	internal class Primes
	{
		private readonly object m_lock = new object();

		public void GenImage( Settings settings )
		{
            var numbers = LoadNumberSet("C:\\GitHub\\Moire\\primes_100k.csv");

            double imageRatio = settings.ImageRatio;
			int width = (int)(settings.Width * imageRatio);
			int height = settings.Height;

            var format = PixelFormat.Format4bppIndexed; // Format1bppIndexed
            Bitmap image = new Bitmap(width, height, format);

            // Lock the bitmap for direct memory access
            BitmapData data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite,
                format);

            // Setup the palette
            SetPaletteColor(image, 0, Color.White);
            SetPaletteColor(image, 1, Color.Black);
            SetPaletteColor(image, 2, Color.LightSkyBlue);

            // Cycle through all the pixels and calculate the color.
            double bounds = settings.Bounds;
			double xoff = settings.XOff;
			double yoff = settings.YOff;
			Parallel.For( 0, width, i =>
			{
                if (i % 20 == 0)
                    Console.WriteLine($"Processing Column {i}");

                for (int j = 0; j < height; j++)
                {
                    int block_i, block_j;  
                    pixelToBlock( settings, i, j, out block_i, out block_j);

                    bool isBlack = CalcColor(settings, block_i, block_j) == Color.Black;
                    bool isPrime = numbers.Contains(block_i+1); // ugh, make indexing more clear
                    //SetPixel1bpp(data, i, j, isBlack);

                    if (isBlack)
                    {
                        SetPixel4bpp(data, i, j, 1);

                    }
                    else if( isPrime )
                    {
                        SetPixel4bpp(data, i, j, 2);
                    }
                    else
                    {
                        SetPixel4bpp(data, i, j, 0);
                    }
				}
			} );

            image.UnlockBits(data);

            image.Save( settings.FileName, ImageFormat.Png );
		}

        private static void SearchMaxImageSize()
        {
            int lo = 1, hi = 65536, max = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                try
                {
                    var bmp = new Bitmap(mid, mid, PixelFormat.Format4bppIndexed);
                    max = mid;
                    lo = mid + 1;
                }
                catch { hi = mid - 1; }
            }
            Console.WriteLine($"Max square dimension: {max}");
        }

        public static HashSet<int> LoadNumberSet(string filePath)
        {
            var set = new HashSet<int>();
            foreach (var line in File.ReadLines(filePath))
            {
                if (int.TryParse(line.Trim(), out int value))
                    set.Add(value);
            }
            return set;
        }

        private void pixelToBlock(Settings settings, int i, int j, out int block_i, out int block_j)
        {
            double a = (double)i / settings.BlockNumPixels;
            double b = (double)j / settings.BlockNumPixels;
            block_i = (int)Math.Floor(a);
            block_j = (int)Math.Floor(b);
        }

		private Color CalcColor( Settings settings, int i, int j )
		{
            int offset = (int)(Math.Pow(2, 16));
            // tried to offset i by above, but not working.
            return (i+1) % (j+1) == 0 ? Color.Black : Color.White;
        }

        /// <summary>
        /// Sets a color in the palette (color table) of a Format4bppIndexed bitmap.
        /// </summary>
        public static void SetPaletteColor(Bitmap bmp, int index, Color color)
        {
            ColorPalette palette = bmp.Palette;       // Gets a COPY — must write it back
            palette.Entries[index] = color;
            bmp.Palette = palette;                    // Reassign to apply changes
        }

        // Set a pixel in a 1bpp bitmap
        static void SetPixel1bpp(BitmapData data, int x, int y, bool isBlack)
        {
            int stride = data.Stride; // Bytes per row (may include padding)
            IntPtr scan0 = data.Scan0;

            // Calculate byte position
            int byteIndex = (y * stride) + (x >> 3); // x >> 3 = x / 8
            byte mask = (byte)(0x80 >> (x & 0x7));   // Bit mask for pixel

            // Get pointer to the byte
            byte[] pixelData = new byte[1];
            Marshal.Copy(scan0 + byteIndex, pixelData, 0, 1);

            if (isBlack)
                pixelData[0] |= mask;  // Set bit to 1
            else
                pixelData[0] &= (byte)~mask; // Clear bit to 0

            // Write back the modified byte
            Marshal.Copy(pixelData, 0, scan0 + byteIndex, 1);
        }

        public static void SetPixel4bpp(BitmapData bmpData, int x, int y, int paletteIndex)
        {
            int byteIndex = y * bmpData.Stride + (x / 2);
            byte b = System.Runtime.InteropServices.Marshal.ReadByte(bmpData.Scan0, byteIndex);

            // High nibble = even x, low nibble = odd x
            if (x % 2 == 0)
                b = (byte)((b & 0x0F) | ((paletteIndex & 0x0F) << 4));
            else
                b = (byte)((b & 0xF0) | (paletteIndex & 0x0F));

            System.Runtime.InteropServices.Marshal.WriteByte(bmpData.Scan0, byteIndex, b);
        }
    }
}
