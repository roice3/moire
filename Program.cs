using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moire
{
	internal class Program
	{
		static void Main( string[] args )
		{
			double bounds = 50;
			//bounds = 35;		//eisenstein
			//bounds = 450;       // used for super-wide of squared picture
			//bounds = 10;
			// linear or fractional powers need huge bounds
			//bounds = 1;	// hyperbolic
			//bounds = 20;

			//int size = 125;
			//size = 5000;
			//int size = (int)bounds * 50;

			int size = 2000;
			//size = 22500;
			//size = 4000;
			bounds = 25;
			//bounds = 100;

			var mSettings = new Settings()
			{
				Width = size,
				Height = size,
				Bounds = bounds,
				FileName = "moire.png",
				Antialias = false   // Turning this on can make all kinds of other moire effects I need to understand
									// and when we are blocking into larger pixels, it doesn't even do anything.
			};

			mSettings.BlockNumPixels = .25; // size / m.BlockNumPixels needs to be constant
			mSettings.BlockNumPixels = 8;
			mSettings.BlockNumPixels = 10;      // 2000
			//mSettings.BlockNumPixels = 15;      // 3000
			//mSettings.BlockNumPixels = 20;		// 4000 bounds 25, 22500 bounds 100
			//mSettings.BlockNumPixels = 100;

			Moire m = new Moire();

			mSettings.ImageRatio = 16.0 / 9;

			mSettings.FileName = "moire_square.png";
			m.m_quantizer.QuantizeShape = R3.Geometry.EQuantizeShape.Gaussian;
			m.GenImage( mSettings );

			mSettings.FileName = "moire_hex.png";
			m.m_quantizer.QuantizeShape = R3.Geometry.EQuantizeShape.Eisenstein_Hex;
			m.GenImage( mSettings );

			mSettings.FileName = "moire_tri.png";
			m.m_quantizer.QuantizeShape = R3.Geometry.EQuantizeShape.Eisenstein_Tri;
			m.GenImage( mSettings );

			mSettings.FileName = "moire_voronoi.png";
			m.m_quantizer.QuantizeShape = R3.Geometry.EQuantizeShape.Voronoi;
			m.GenImage( mSettings );

			mSettings.FileName = "moire_mixed.png";
			m.m_quantizer.QuantizeShape = R3.Geometry.EQuantizeShape.Gaussian;
			m.m_quantizer.MixShapes = true;
			m.GenImage( mSettings );
		}
	}
}
