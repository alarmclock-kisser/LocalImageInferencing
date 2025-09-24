using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LocalImageInferencing.Core
{
	public class ImageObj : IDisposable
	{
		public Guid Id { get; private set; } = Guid.Empty;
		public DateTime CreatedAt { get; private set; } = DateTime.Now;
		public string FilePath { get; private set; } = string.Empty;

		public IEnumerable<Image<Rgba32>> Frames { get; private set; } = [];
		public IEnumerable<SixLabors.ImageSharp.Size> Sizes { get; private set; } = [];
		public int FramesCount => this.Frames.Count();

		public int Channels => 4;
		public int BitsPerChannel => 8;
		public int BitsPerPixel => this.Channels * this.BitsPerChannel;

		public float[] FrameSizesMb => this.Sizes.Select(s => (s.Width * s.Height * this.Channels) / (1024f * 1024f)).ToArray();
		public float[] FrameBase64SizesMb => this.FrameSizesMb.Select(mb => mb * 4f / 3f).ToArray();
		public float ScalingFactor { get; private set; } = 1.0f;

		public bool OnHost { get; set; } = false;
		public bool OnDevice { get; set; } = false;
		public IntPtr Pointer { get; set; } = IntPtr.Zero;



		internal ImageObj(string filePath)
		{
			try
			{
				this.Id = Guid.NewGuid();
				this.FilePath = filePath;

				using var image = Image.Load(filePath);

				if (image.Frames.Count > 1)
				{
					// Multi frame image
					var framesList = new List<Image<Rgba32>>(image.Frames.Count);
					var sizesList = new List<SixLabors.ImageSharp.Size>(image.Frames.Count);
					for (int i = 0; i < image.Frames.Count; i++)
					{
						var frame = image.Frames.CloneFrame(i);
						framesList.Add(frame.CloneAs<Rgba32>());
						sizesList.Add(frame.Size);
					}
					this.Frames = framesList;
					this.Sizes = sizesList;
				}
				else
				{
					// Static image
					var singleFrame = image.CloneAs<Rgba32>();
					this.Frames = [singleFrame];
					this.Sizes = [singleFrame.Size];
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				this.Dispose();
			}
		}

		internal ImageObj(Size size, Color? color = null, int frames = 1)
		{
			color ??= Color.Black;
			this.Id = Guid.NewGuid();
			this.FilePath = string.Empty;
			var framesList = new List<Image<Rgba32>>(frames);
			var sizesList = new List<SixLabors.ImageSharp.Size>(frames);
			for (int i = 0; i < frames; i++)
			{
				var img = new Image<Rgba32>(size.Width, size.Height);
				img.Mutate(x => x.BackgroundColor(color.Value));
				framesList.Add(img);
				sizesList.Add(img.Size);
			}
			this.Frames = framesList;
			this.Sizes = sizesList;
		}



		public byte[] GetPixels(int frameId = 0)
		{
			if (frameId < 0 || frameId >= this.Frames.Count())
			{
				return [];
			}

			var frame = this.Frames.ElementAt(frameId);
			var pixelBytes = new byte[frame.Width * frame.Height * 4];
			frame.CopyPixelDataTo(pixelBytes);

			return pixelBytes;
		}

		public Image<Rgba32>? SetPixels(byte[] pixels, int frameId = 0)
		{
			if (frameId < 0 || frameId >= this.Frames.Count())
			{
				return null;
			}

			var frame = this.Frames.ElementAt(frameId);
			var size = frame.Size;

			if (pixels.Length != frame.Width * frame.Height * 4)
			{
				return null;
			}

			frame.ProcessPixelRows(accessor =>
			{
				for (int y = 0; y < accessor.Height; y++)
				{
					var pixelRow = accessor.GetRowSpan(y);
					for (int x = 0; x < accessor.Width; x++)
					{
						int index = (y * size.Width + x) * this.Channels;
						pixelRow[x] = new Rgba32(
							pixels[index],     // R
							pixels[index + 1], // G
							pixels[index + 2], // B
							pixels[index + 3]  // A
						);
					}
				}
			});

			this.Frames = this.Frames.Take(frameId)
				.Append(frame)
				.Concat(this.Frames.Skip(frameId + 1));

			return frame;
		}

		public string? GetBase64String(int frameId = 0, string format = "png")
		{
			if (frameId < 0 || frameId >= this.Frames.Count())
			{
				return null;
			}

			var frame = this.Frames.ElementAt(frameId);
			using var ms = new MemoryStream();
			
			switch(format.ToLower())
			{
				case "png":
					frame.SaveAsPng(ms);
					break;
				case "jpeg":
				case "jpg":
					frame.SaveAsJpeg(ms);
					break;
				case "bmp":
					frame.SaveAsBmp(ms);
					break;
				case "gif":
					frame.SaveAsGif(ms);
					break;
				case "tga":
					frame.SaveAsTga(ms);
					break;
				case "tiff":
				case "tif":
					frame.SaveAsTiff(ms);
					break;
				default:
					frame.SaveAsPng(ms);
					break;
			}

			var base64String = Convert.ToBase64String(ms.ToArray());

			return base64String;
		}


		public async Task Downscale(float scaleFactor = 0.5f)
		{
			if (scaleFactor <= 0 || scaleFactor >= 1)
			{
				return;
			}

			// Lokale Kopien für Parallelisierung
			var frames = this.Frames;
			var tasks = new List<Task<(Image<Rgba32> Frame, SixLabors.ImageSharp.Size Size)>>(frames.Count());

			foreach (var frame in frames)
			{
				tasks.Add(Task.Run(() =>
				{
					int newWidth = Math.Max(1, (int) (frame.Width * scaleFactor));
					int newHeight = Math.Max(1, (int) (frame.Height * scaleFactor));

					var resizedFrame = frame.Clone(ctx => ctx.Resize(newWidth, newHeight));
					var size = resizedFrame.Size;

					frame.Dispose();

					return (resizedFrame, size);
				}));
			}

			// Warten bis alle fertig sind
			var results = await Task.WhenAll(tasks);

			// Ergebnisse in neue Listen übernehmen
			var newFrames = new List<Image<Rgba32>>(results.Length);
			var newSizes = new List<SixLabors.ImageSharp.Size>(results.Length);

			foreach (var (frame, size) in results)
			{
				newFrames.Add(frame);
				newSizes.Add(size);
			}

			this.Frames = newFrames;
			this.Sizes = newSizes;
			this.ScalingFactor *= scaleFactor;
		}





		public void Dispose()
		{
			foreach (var img in this.Frames)
			{
				img.Dispose();
			}

			this.Frames = [];
			GC.SuppressFinalize(this);
		}
	}
}
