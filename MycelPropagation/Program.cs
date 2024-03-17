using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.ColorSpaces;

using MycelPropagation;

class Debug
{
	public static void Main()
	{			
		var mycel = new MycelPropagationMap(
						mapSize: 500,
						obstacles: new MycelObstacle[]
						{
							new(
								attenuation: 1f,
								points: new PointF[]
								{
									new(100, 130),
									new(300, 130),
									new(300, 170),
									new(100, 170)
								}),
							new(
								attenuation: 1f,
								points: new PointF[]
								{
									new(100, 130),
									new(140, 130),
									new(140, 300),
									new(100, 300)
								}),
							new(
								attenuation: 0.75f,
								points: new PointF[]
								{
									new(350, 110),
									new(475, 110),
									new(475, 210),
									new(350, 210)
								}),
							new(
								attenuation: 0.75f,
								points: new PointF[]
								{
									new(350, 360),
									new(475, 340),
									new(475, 460),
									new(350, 480)
								}),
							new(
								attenuation: 0.5f,
								points: new PointF[]
								{
									new(280, 300),
									new(420, 300),
									new(420, 340),
									new(280, 340)
								}),
							new(
								attenuation: 0.5f,
								points: new PointF[]
								{
									new(100, 345),
									new(120, 365),
									new(40, 450),
									new(15, 430)
								}),
							new(
								attenuation: 1f,
								points: new PointF[]
								{
									new(100, 430),
									new(300, 430),
									new(300, 470),
									new(100, 470)
								}),
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(100, 100),
							// 		new(240, 100),
							// 		new(240, 150),
							// 		new(100, 150)
							// 	}),
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(100, 100),
							// 		new(150, 100),
							// 		new(150, 400),
							// 		new(100, 400)
							// 	}),
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(100, 200),
							// 		new(225, 200),
							// 		new(225, 250),
							// 		new(100, 250)
							// 	}),
								
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(300, 100),
							// 		new(440, 100),
							// 		new(440, 150),
							// 		new(300, 150)
							// 	}),
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(300, 100),
							// 		new(350, 100),
							// 		new(350, 400),
							// 		new(300, 400)
							// 	}),
							// new(
							// 	attenuation: 1f,
							// 	points: new PointF[]
							// 	{
							// 		new(300, 200),
							// 		new(425, 200),
							// 		new(425, 250),
							// 		new(300, 250)
							// 	}),
						},
						FirstPassCircularity: Circularity.OCTOTHORPE
						);
						
		mycel.DebugSaveObstacleMap("../../../obMap.png");
		
		while (!mycel.PropagationMapExists())
		{
			Thread.Sleep(100);
		}
		mycel.DebugSavePropagationMap("../../../prMap.png");
		mycel.DebugSavePropagationGIF("../../../prGrow.gif");
	}
}

