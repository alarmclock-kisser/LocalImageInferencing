using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalImageInferencing.Core
{
	public class ImageCollection : IDisposable
	{
		public static string[] SupportedFormats = ["png", "jpg", "jpeg", "bmp", "gif", "tif", "tiff", "tga", "png"];

		private ConcurrentDictionary<Guid, ImageObj> images = [];
		public int Count => this.images.Count;
		public IEnumerable<ImageObj> Images => this.images.Values;

		public int MaxAgeSeconds { get; private set; } = 300;



		public ImageObj? this[Guid id]
		{
			get
			{
				if (this.images.TryGetValue(id, out var img))
				{
					return img;
				}
				else
				{
					return null;
				}
			}
		}



		public ImageCollection(int maxAgeSeconds = 300)
		{
			this.MaxAgeSeconds = maxAgeSeconds;
		}


		public ImageObj? LoadFromFile(string filePath)
		{
			try
			{
				var img = new ImageObj(filePath);
				if (this.images.TryAdd(img.Id, img))
				{
					return img;
				}
				else
				{
					img.Dispose();
					return null;
				}
			}
			catch
			{
				return null;
			}
		}

		public ImageObj? CreateNew(Size size, Color? color = null, int frames = 1)
		{
			try
			{
				var img = new ImageObj(size, color, frames);
				if (this.images.TryAdd(img.Id, img))
				{
					return img;
				}
				else
				{
					img.Dispose();
					return null;
				}
			}
			catch
			{
				return null;
			}
		}

		



		public bool Remove(Guid id)
		{
			if (this.images.TryRemove(id, out var img))
			{
				img.Dispose();
				return true;
			}

			return false;
		}

		public void ClearAll()
		{
			foreach (var img in this.images.Values)
			{
				img.Dispose();
			}

			this.images.Clear();
		}



		public void Dispose()
		{
			foreach (var img in this.images.Values)
			{
				img.Dispose();
			}

			this.images.Clear();
			GC.SuppressFinalize(this);
		}



		public static SixLabors.ImageSharp.Size GetSharpSize(int height, int width)
		{
			width = Math.Clamp(width, 1, 32768);
			height = Math.Clamp(height, 1, 32768);

			return new SixLabors.ImageSharp.Size(width, height);
		}

		public static SixLabors.ImageSharp.Color? GetSharpColor(System.Drawing.Color color)
		{
			if (color == System.Drawing.Color.Empty)
			{
				return null;
			}

			return SixLabors.ImageSharp.Color.FromRgba(color.R, color.G, color.B, color.A);
		}

		public static SixLabors.ImageSharp.Color GetSharpColor(string hexColor = "#00000000")
		{
			if (string.IsNullOrWhiteSpace(hexColor))
			{
				hexColor = "#00000000";
			}
			if (!hexColor.StartsWith("#"))
			{
				hexColor = "#" + hexColor;
			}
			try
			{
				return SixLabors.ImageSharp.Color.ParseHex(hexColor);
			}
			catch
			{
				return SixLabors.ImageSharp.Color.FromRgba(0, 0, 0, 0);
			}
		}

		public static System.Drawing.Color GetDrawingColor(SixLabors.ImageSharp.Color color)
		{
			var rgba = color.ToPixel<Rgba32>();
			return System.Drawing.Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B);
		}
	}
}
