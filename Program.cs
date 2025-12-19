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

			int size = 2500;
			bounds = 25;

			var mSettings = new Settings()
			{
				Width = size,
				Height = size,
				Bounds = bounds,
				FileName = "moire.png",
				Antialias = false   // Turning this on can make all kinds of other moire effects I need to understand
			};

			mSettings.BlockNumPixels = .25; // size / m.BlockNumPixels needs to be constant
			mSettings.BlockNumPixels = 8;
			mSettings.BlockNumPixels = 10;      // 2000
			mSettings.BlockNumPixels = 15;      // 2500
			//mSettings.BlockNumPixels = 20;
			//mSettings.BlockNumPixels = 100;

			Moire m = new Moire();
			m.GenImage( mSettings );
			return;
		}
	}
}
