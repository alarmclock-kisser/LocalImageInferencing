using LocalImageInferencing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalImageInferencing.Shared
{
	public class ImageObjInfo
	{
		public Guid Id { get; set; } = Guid.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.MinValue;
		public string FilePath { get; set; } = string.Empty;

		public IEnumerable<(int Width, int Height)> Sizes { get; set; } = [];
		public int FramesCount { get; set; } = 0;
		public int Channels { get; set; } = 4;
		public int BitsPerChannel { get; set; } = 8;
		public int BitsPerPixel { get; set; } = 32;

		public bool OnHost { get; set; } = false;
		public bool OnDevice { get; set; } = false;
		public string Pointer { get; set; } = "0";



		public ImageObjInfo()
		{
			// Parameterless constructor for serialization
		}

		[JsonConstructor]
		public ImageObjInfo(ImageObj? obj)
		{
			if (obj == null)
			{
				return;
			}

			this.Id = obj.Id;
			this.CreatedAt = obj.CreatedAt;
			this.FilePath = obj.FilePath;
			this.Sizes = obj.Sizes.Select(s => (s.Width, s.Height));
			this.FramesCount = obj.FramesCount;
			this.Channels = obj.Channels;
			this.BitsPerChannel = obj.BitsPerChannel;
			this.BitsPerPixel = obj.BitsPerPixel;
			this.OnHost = obj.OnHost;
			this.OnDevice = obj.OnDevice;
			this.Pointer = obj.Pointer.ToString();
		}




	}
}
