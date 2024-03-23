using System.Diagnostics;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace MycelPropagation;

public class MycelObstacle
{
	public PointF[] points;
	
	/// <summary> How difficult propagation is through this surface. 
	/// <c>0.0</c> is no additional difficulty, 
	/// <c>0.5</c> is twice as difficult,
	/// <c>1.0</c> means object will entirely occlude propagation</summary>
	public float attenuation;

	public MycelObstacle(float attenuation, PointF[] points)
	{
		this.points = points;
		this.attenuation = attenuation;
	}
}

/// <summary>
/// Determines the shape used to grow the frontier. More complex shapes result in a more round propagation, at the cost of performance.
/// </summary>
public enum Circularity
{
	/// <summary> 4 operations per pixel </summary>
	DIAMOND,
	/// <summary> 8 operations per pixel </summary>
	SQUARE,
	/// <summary> 16 operations per pixel </summary>
	OCTOTHORPE,
	/// <summary> 32 operations per pixel </summary>
	OCTOPRIME
}

class PixelFrontier
{
	private ushort lowestValue = 0; 
	private readonly Dictionary<ushort, List<(int X, int Y)>> frontier = new();
	
	public void Add(ushort value, (int X, int Y) pos)
	{
		try
		{	
			frontier[value].Add(pos);
		}
		catch(KeyNotFoundException)
		{
			frontier[value] = new()
			{
				pos
			};
		}
	}
	
	public void Move(ushort from, ushort to, (int X, int Y) pos)
	{
		Remove(from, pos);
		try
		{	
			frontier[to].Add(pos);
		}
		catch(KeyNotFoundException)
		{
			frontier[to] = new()
			{
				pos
			};
		}
	}
	
	/// <summary>
	/// Removes the pixel from the frontier dictionary.
	/// </summary>
	public void Remove(ushort value, (int X, int Y) pos)
	{
		frontier[value].Remove(pos);
		
		if(frontier[value].Count == 0)
		{
			frontier.Remove(value);
			
			lowestValue = frontier.Keys.Min();
		}
	}
	
	public (int X, int Y) GetNext(out ushort value)
	{
		value = lowestValue;
		var pixel = frontier[value].First();
		
		return pixel;
	}
}

public class MycelPropagationMap
{
	/// <summary> Height and width, in pixels, of the MycelPropagation map and atlases <summary>
	private int mapSize;
	private Image<L16> obstacleMap;
	private Image<L16>? propagationMap = null;
	private ushort maxPropagationValue = 0;
	
	/// <summary>
	/// Creates a new MycelPropagationMap
	/// </summary>
	/// <param name="mapSize"> Height and width, in pixels, of the MycelPropagation map and atlases </param>
	/// <param name="FirstPassCircularity"> Circularity type to use for the initial propagation map generation </param>
	public MycelPropagationMap(int mapSize, MycelObstacle[] obstacles, Circularity FirstPassCircularity = Circularity.SQUARE)
	{
		this.mapSize = mapSize;
		obstacleMap = new(mapSize, mapSize);
		
		foreach (var obstacle in obstacles)
		{
			obstacleMap.Mutate(x => x.FillPolygon(new Color(new L16((ushort)(65535 * obstacle.attenuation))), obstacle.points));
		}
		
		Console.WriteLine("generating prop map");
		Task.Run(() => GeneratePropagationMap(1000000, FirstPassCircularity));
	}
	
	public void GeneratePropagationMap(int MaxIterations = 1000000, Circularity circularity = Circularity.SQUARE)
	{
		var sw = Stopwatch.StartNew();
		var front = new PixelFrontier();
		var map = new Image<L16>(mapSize, mapSize, new L16(65535));
		
		int center = mapSize/2;
		front.Add(0, (center, center));
		
		void AddToFrontier(ushort startvalue, ushort distance, (int X, int Y) pos)
		{
			if (pos.X < 0 || pos.X > mapSize-1 || pos.Y < 0 || pos.Y > mapSize-1) {return;}
			
			var oldval = map[pos.X, pos.Y].PackedValue;
			var atten = obstacleMap[pos.X, pos.Y].PackedValue;
			
			if (atten == 65535)
			{ return; }
			else
			{
				distance = (ushort)(distance * 65535f/(65535f - atten));
			}
			
			// C# handles ushorts very horribly. This check is neccesary to prevent overflow.
			int valueUNSAFE = startvalue + distance;
			ushort value = (ushort)(valueUNSAFE < 65535 ? valueUNSAFE : 65535);  
			
			if (value >= oldval)
			{ return; }
			else if (oldval == 65535)
			{
				front.Add(value, pos);
				
				if (value > maxPropagationValue) maxPropagationValue = value;
			}
			else
			{
				front.Move(oldval, value, pos);
			}
			map[pos.X, pos.Y] = new L16(value);
		}
		
		var (X, Y) = (center, center);
		for (int i = 0; i < MaxIterations; i++)
		{
			(X, Y) = front.GetNext(out ushort value);
			
			switch (circularity)
			{
				case Circularity.DIAMOND:
					AddToFrontier(value, 10, (X + 1, Y));
					AddToFrontier(value, 10, (X, Y + 1));
					AddToFrontier(value, 10, (X - 1, Y));
					AddToFrontier(value, 10, (X, Y - 1));
					break;
				case Circularity.SQUARE:
					AddToFrontier(value, 14, (X + 1, Y + 1));
					AddToFrontier(value, 10, (X + 1, Y));
					AddToFrontier(value, 14, (X + 1, Y - 1));
					AddToFrontier(value, 10, (X, Y + 1));
					AddToFrontier(value, 10, (X, Y - 1));
					AddToFrontier(value, 14, (X - 1, Y + 1));
					AddToFrontier(value, 10, (X - 1, Y));
					AddToFrontier(value, 14, (X - 1, Y - 1));
					break;
				case Circularity.OCTOTHORPE:
					AddToFrontier(value, 14, (X + 1, Y + 1));
					AddToFrontier(value, 10, (X + 1, Y));
					AddToFrontier(value, 14, (X + 1, Y - 1));
					AddToFrontier(value, 10, (X, Y + 1));
					AddToFrontier(value, 10, (X, Y - 1));
					AddToFrontier(value, 14, (X - 1, Y + 1));
					AddToFrontier(value, 10, (X - 1, Y));
					AddToFrontier(value, 14, (X - 1, Y - 1));
					
					AddToFrontier(value, 22, (X - 1, Y + 2));
					AddToFrontier(value, 22, (X + 1, Y + 2));
					AddToFrontier(value, 22, (X + 2, Y + 1));
					AddToFrontier(value, 22, (X + 2, Y - 1));
					AddToFrontier(value, 22, (X + 1, Y - 2));
					AddToFrontier(value, 22, (X - 1, Y - 2));
					AddToFrontier(value, 22, (X - 2, Y - 1));
					AddToFrontier(value, 22, (X - 2, Y + 1));
					break;
				default:
					throw new NotImplementedException(circularity.ToString());
			}
			
			try
			{
				front.Remove(value, (X, Y));
			}
			catch (InvalidOperationException)
			{
				//finished
				break;
			}
		}
		
		propagationMap = map;
		Console.WriteLine($"Created prop map in: {sw.Elapsed:ss'.'ffff}. Max propagation: {maxPropagationValue}");
	}
		
	public Image<L16> GetPropagation(ushort threshold)
	{
		if(!PropagationMapExists()) throw new InvalidOperationException("Propagation map has not yet been generated.");

		L16 upper = new(0);
		L16 lower = new(65535);	
		Vector4 upperVec4 = upper.ToVector4();
		Vector4 lowerVec4 = lower.ToVector4();	
		
		//return propagationMap!.Clone(x => x.BinaryThreshold(threshold, upper, lower));  //This doesn't work since binarisation converts it to L8 >: (
		return propagationMap!.Clone( x => x.ProcessPixelRowsAsVector4(row =>
		{

			for (int x = 0; x < row.Length; x++)
			{
				L16 lumi = new();
				lumi.FromScaledVector4(row[x]);
				row[x] = lumi.PackedValue > threshold ? upperVec4 : lowerVec4;
			}
		}));
	}
	
	public bool PropagationMapExists()
	{
		return propagationMap is not null;
	}
	
	public void DebugSavePropagationGIF(string path)
	{
		using Image<L16> gif = new(mapSize, mapSize);
		
		var gifMetaData = gif.Metadata.GetGifMetadata();
		gifMetaData.RepeatCount = 0;
		GifFrameMetadata metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
		metadata.FrameDelay = 20;
		
		for (ushort i = 0; i < maxPropagationValue; i += 50)
		{
			using var image = GetPropagation(i);
			
			metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
			metadata.FrameDelay = 20;

			gif.Frames.AddFrame(image.Frames.RootFrame);
		}
		
		gif.SaveAsGif(path);
		Console.WriteLine($"Gif saved at {path}");
	}
	
	public void DebugSaveObstacleMap(string path)
	{
		obstacleMap.SaveAsPng(path);
		Console.WriteLine($"Image saved at {path}");
	}
		
	public void DebugSavePropagationMap(string path)
	{
		propagationMap.SaveAsPng(path);
		Console.WriteLine($"Image saved at {path}");
	}
}

