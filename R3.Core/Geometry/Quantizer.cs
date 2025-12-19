namespace R3.Geometry
{
	using Moire;
	using System;
	using System.Numerics;
	using Math = System.Math;

	public enum EQuantizeShape
	{
		Gaussian,
		Eisenstein_Hex,
		Eisenstein_Tri,
		Voronoi
	}

	public class Quantizer
	{
		public EQuantizeShape QuantizeShape { get; set; } = EQuantizeShape.Gaussian;

		private double m_quantaSize { get ; set; }

		public Vector3D Quantize( double quantaSize, Vector3D v )
		{
			/* // This code will show all quantizations in separate quadrants.
			if( v.Y < 0 && v.X < 0 )
				QuantizeSphape = EQuantizeShape.Eisenstein_Hex;
			else if( v.Y < 0 && v.X > 0 )
				QuantizeSphape = EQuantizeShape.Eisenstein_Tri;
			else if( v.Y > 0 && v.X < 0 )
				QuantizeSphape = EQuantizeShape.Gaussian;
			else
				QuantizeSphape = EQuantizeShape.Voronoi;
			*/

			m_quantaSize = quantaSize;

			// Round to closest grid point.
			Vector3D quantized = v;
			switch( QuantizeShape )
			{
				case EQuantizeShape.Gaussian:
					quantized = RoundGaussian( v );
					break;
				case EQuantizeShape.Eisenstein_Hex:
					quantized = RoundEisenstein( v );
					break;
				case EQuantizeShape.Eisenstein_Tri:
					quantized = RoundEisenstein( v );
					break;
				case EQuantizeShape.Voronoi:
					// Neartree makes our grid. Ugh, too slow once I scale.
					NearTreeObject closest;
					m_grid.FindNearestNeighbor( out closest, v, 1 );
					if( closest == null )
						return v;
					quantized = closest.Location;
					break;
			}

			// TODO: Spherical or Hyperbolic tiling pixels? On a "geodesic sphere" grid maybe??

			return quantized;
		}

		// For voronoi calculations.
		private NearTree m_grid;

		private double RandInBounds( Random random, double bounds )
		{
			double randDouble = random.NextDouble();
			double scaled = (randDouble - 0.5) * bounds * 2;
			return scaled;
		}

		public void SetupGrid( int numPoints, double bounds )
		{
			m_grid = new NearTree( Metric.Euclidean );
			int id = 0;

			// Random
			Random random = new Random();
			for( int i = 0; i < numPoints; i++ )
			{
				Complex location = new Complex( RandInBounds( random, bounds ), RandInBounds( random, bounds ) );
				m_grid.InsertObject( new NearTreeObject() { ID = id, Location = Vector3D.FromComplex( location ) } );
			}
		}

		private Vector3D RoundGaussian( Vector3D v )
		{
			Vector3D result = new Vector3D(
				Quantize( v.X, m_quantaSize ),
				Quantize( v.Y, m_quantaSize ) );
			return result;
		}

		/// <summary>
		/// Quantize a double input.
		/// </summary>
		private double Quantize( double input, double quantaSize )
		{
			return Math.Round( input / quantaSize ) * quantaSize;
		}

		private Vector3D RoundGaussianDev( Vector3D v )
		{
			// sensitive to relative size of xoff
			// I think we want this a bit bigger than the size of a pixel.
			int awayFromZero = 1;
			//digits = (int)(-Math.Log( settings.XOff, 10 ));
			//awayFromZero = 0;

			// hmmm, maybe it is more a function of bounds?

			// Question: Does number of pixels play in?
			// NO! (at least not if we have the squares from rounding bigger than pixels)
			// I made identical 500, 1500, and 6000 pixel pictures when rounding to 0 digits.

			// Round to a fraction 1/n with this.
			// n = 10 and awaFromZero = 0 is the same as n = 1 and awayFromZero = 1.
			// n = 100 and awaFromZero = 0 is the same as n = 1 and awayFromZero = 2.
			double n = 1;   // Let's go slowly from 1 to 100;
							//double n = 30;
							//n = 5;	// good with awayFromZero = 1;

			Vector3D result = new Vector3D(
				Math.Round( v.X * n, awayFromZero ) / n,
				Math.Round( v.Y * n, awayFromZero ) / n );
			return result;
		}

		private Vector3D RoundEisenstein( Vector3D v )
		{
			double realPart = v.X;
			double imaginaryPart = v.Y;

			// Round to nearest Eisenstein integer
			var (a, b) = RoundToNearestEisenstein( realPart, imaginaryPart, m_quantaSize );

			var location = ComputeLocationFromEisenstein( a, b );
			return Vector3D.FromComplex( location );
		}

		// Constants for ω = -1/2 + i√3/2
		// We scale this if we want to change the grid size.
		Complex m_omega = new Complex( -0.5, Math.Sqrt( 3 ) / 2 ); // Primitive cube root of unity

		private Complex ComputeLocationFromEisenstein( double a, double b )
		{
			Complex location = (new Complex( a, 0 ) + m_omega * b);
			return location;
		}

		private (double a, double b) ComputeEisensteinFromLocation( double real, double imag )
		{
			// Compute the Eisenstein coordinates (a, b)
			double a = real - m_omega.Real * imag / m_omega.Imaginary;
			double b = imag / m_omega.Imaginary;
			return (a, b);
		}

		private (double a, double b) RoundToNearestEisenstein( double real, double imag, double quantaSize )
		{
			var loc = ComputeEisensteinFromLocation( real, imag );
			double a = loc.a;
			double b = loc.b;

			/* experimentation
			// Round to nearest... see notes above...
			int awayFromZero = 0;
			double n = 7;
			n = 5;
			//awayFromZero = 2; n = 4;  // hyperbolic (ugh, what did I mean by using this term?)  I will say, it rounds to pixels small enough when bounds are 50, that it reverts back to gaussian.
			double roundedA = Math.Round( a * n, awayFromZero ) / n;
			double roundedB = Math.Round( b * n, awayFromZero ) / n;
			*/

			double roundedA = Quantize( a, quantaSize );
			double roundedB = Quantize( b, quantaSize );

			/* // Testing
			quantaSize = 1;
			double ta = -.5;
			double tb = .5;
			roundedA = Quantize( ta, quantaSize );
			roundedB = Quantize( tb, quantaSize );
			Complex t = ComputeLocationFromEisenstein( ta, tb );
			var tc = CheckForCloser( ta, tb, roundedA, roundedB );
			*/


			//return (roundedA, roundedB);
			if( QuantizeShape == EQuantizeShape.Eisenstein_Hex )
				return CheckForCloser( a, b, roundedA, roundedB, quantaSize );  // Voronoi hexagons

			if( QuantizeShape == EQuantizeShape.Eisenstein_Tri )
				return ClosestHexVert( a, b, roundedA, roundedB, quantaSize );

			throw new System.ArgumentException();
		}

		private (double a, double b) CheckForCloser( double a, double b, double roundedA, double roundedB, double quantaSize )
		{
			Complex p = ComputeLocationFromEisenstein( a, b );
			double ra = 0, rb = 0;
			double closest = double.MaxValue;
			for( int i = -1; i <= 1; i++ )
				for( int j = -1; j <= 1; j++ )
				{
					double ta = roundedA + i * quantaSize;
					double tb = roundedB + j * quantaSize;

					Complex t = ComputeLocationFromEisenstein( ta, tb );
					double d = (p - t).Magnitude;
					if( d < closest )
					{
						ra = ta;
						rb = tb;
						closest = d;
					}
				}

			return (ra, rb);
		}

		private (double a, double b) ClosestHexVert( double a, double b, double roundedA, double roundedB, double quantaSize )
		{
			Complex point = ComputeLocationFromEisenstein( a, b );
			Complex rounded = ComputeLocationFromEisenstein( roundedA, roundedB );
			double ra = 0, rb = 0;
			double closest = double.MaxValue;
			for( int i = 0; i < 6; i++ )
			{
				Vector3D ray = new Vector3D( quantaSize * .5 / Math.Cos( Math.PI / 6 ), 0 );
				ray.RotateXY( new Vector3D(), Math.PI / 6 + 2 * Math.PI / 6 * i );

				Complex test = rounded + ray.ToComplex();
				double d = (point - test).Magnitude;
				if( d < closest )
				{
					// How to get the a,b values of test???
					var loc = ComputeEisensteinFromLocation( test.Real, test.Imaginary );
					ra = loc.a;
					rb = loc.b;

					closest = d;
				}
			}

			return (ra, rb);
		}
	}
}
