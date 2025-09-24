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
					this.Frames = new Image<Rgba32>[image.Frames.Count];
					this.Sizes = new SixLabors.ImageSharp.Size[image.Frames.Count];
					for (int i = 0; i < image.Frames.Count; i++)
					{
						var frame = image.Frames.CloneFrame(i);
						this.Frames = this.Frames.Append(frame.CloneAs<Rgba32>());
						this.Sizes = this.Sizes.Append(frame.Size);
					}
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
			
			this.Frames = new Image<Rgba32>[frames];
			this.Sizes = new SixLabors.ImageSharp.Size[frames];
			for (int i = 0; i < frames; i++)
			{
				var img = new Image<Rgba32>(size.Width, size.Height);
				img.Mutate(x => x.BackgroundColor(color.Value));
				this.Frames = this.Frames.Append(img);
				this.Sizes = this.Sizes.Append(img.Size);
			}
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
