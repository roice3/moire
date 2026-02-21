namespace Moire
{
	using R3.Core;
	using R3.Geometry;
	using R3.Math;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Threading.Tasks;
	using Math = System.Math;

	internal enum EMetric
	{
		Euclidean,
		Spherical,
		Hyperbolic,
		Lorentzian,
	}

	public class Settings
	{
		public Settings() { }

		public int Width { get; set; }
		public int Height { get; set; }
		public double Bounds { get; set; }          // y bounds (x will be scaled accordingly).
		public double ImageRatio { get; set; } = 1.0;
		public string FileName { get; set; }
		public bool Antialias { get; set; }
		public bool EMetric { get; set; }

		/// <summary>
		/// "block size", e.g. how may screen pixels are our virtual "pixels"?
		/// This is the edge-length of the pixel shape.
		/// </summary>
		public double BlockNumPixels { get; set; }

		public double ScreenPixelSize
		{
			get 
			{
				// We go from -bounds to bounds
				return 2 * Bounds / Width;
			}
		}

		private double Aspect
		{
			get
			{
				return (double)Width / Height;
			}
		}

		public double XOff
		{
			get
			{
				return Aspect * Bounds * 2 / (Width - 1);
			}
		}

		public double YOff
		{
			get
			{
				return Bounds * 2 / (Height - 1);
			}
		}
	}

	internal class Moire
	{
		public Quantizer m_quantizer = new Quantizer();
		private readonly object m_lock = new object();

		public void GenImage( Settings settings )
		{
			double imageRatio = settings.ImageRatio;
			double numPoints = (double)settings.Width * imageRatio / settings.BlockNumPixels * settings.Height / settings.BlockNumPixels;
			numPoints *= 3; // artificial, I'm just going a bit by eye with this.
			m_quantizer.SetupGrid( (int)numPoints, settings.Bounds * imageRatio );

			int width = (int)(settings.Width * imageRatio);
			int height = settings.Height;

			Bitmap image = new Bitmap( width, height );

			// Cycle through all the pixels and calculate the color.
			int row = 0;
			double bounds = settings.Bounds;
			double xoff = settings.XOff;
			double yoff = settings.YOff;
			Parallel.For( 0, width, i =>
			{
				if( row++ % 20 == 0 )
					System.Console.WriteLine( string.Format( "Processing Line {0}", row ) );

				for( int j = 0; j < height; j++ )
				{
					double x = -bounds*imageRatio + i * xoff;
					double y = -bounds + j * yoff;

					// Doesn't seem to make any real difference here.
					// Maybe should be in round functions.
					double continuousTranslate = 0.0;
					x += continuousTranslate;
					y += continuousTranslate;

					if( settings.Antialias )
					{
						const int div = 3;
						//const int div = 1;
						List<Color> colors = new List<Color>();
						for( int k = 0; k <= div; k++ )
							for( int l = 0; l <= div; l++ )
							{
								double xa = x - xoff / 2 + k * xoff / div;
								double ya = y - yoff / 2 + l * yoff / div;
								Vector3D v = new Vector3D( xa, ya );

								Color color = CalcColor( settings, v );
								colors.Add( color );
							}

						lock( m_lock )
						{
							Color avg = ColorUtil.AvgColor( colors );
							image.SetPixel( i, j, avg );
						}
					}
					else
					{
						lock( m_lock )
						{
							Vector3D v = new Vector3D( x, y );
							image.SetPixel( i, j, CalcColor( settings, v ) );
						}
					}
				}
			} );

			image.Save( settings.FileName, ImageFormat.Png );
		}

		private Color CalcColor( Settings settings, Vector3D v )
		{
			//v.X += v.Y * Math.Tan( System.Math.PI / 6 );

			double quantaSize = settings.ScreenPixelSize * settings.BlockNumPixels;
			Vector3D quantized = m_quantizer.Quantize( quantaSize, v );

			//double mag = quantized.Abs();
			double ds2 = quantized.X * quantized.X + quantized.Y * quantized.Y;    // euclidean	
			//double ds2 = quantized.X * quantized.X - quantized.Y * quantized.Y;        // lorentz metric
			
			//double dist = Spherical2D.SDist( new Vector3D(), quantized );
			//ds2 = dist * dist;
			//ds2 *= 1000.0;	// scaling.

			//double dist = H3Models.Ball.HDist( new Vector3D(), quantized/20 );
			//ds2 = dist * dist;
			//ds2 *= 100;


			double scaled = ds2;	// use with lorentz??
			//double scaled = mag * mag; // Why is squared so special? maybe because it is the square of the metric??
			//double scaled = Math.Pow( mag, .5 );
			//double scaled = Math.Pow( Math.E, mag );
			//double scaled = Math.Pow( 2.0, mag );
			//double scaled = Math.Pow( mag, .5 ) * 1000;	// fractional powers need be scaled up a ton to get moire
			//double scaled = Math.Log( mag ) * 2500;
			//double scaled = Spherical2D.e2sNorm( mag ) * 2000;
			//double scaled = DonHatch.e2hNorm( mag ) * 2000;

			double quantizedTransform = Math.PI * 0.0;

			double intensity = (1 + Math.Sin( scaled + quantizedTransform )) / 2;

			// spike it
			double spikiness = 3;	// good val is 3
			intensity = Math.Pow( intensity, spikiness );

			Color color = Color.Blue;
			return ColorUtil.AdjustL( color, intensity );
		}
	}
}
