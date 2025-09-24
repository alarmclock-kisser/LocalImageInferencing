using LocalImageInferencing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalImageInferencing.Shared
{
	public class ImageObjData
	{
		public Guid Id { get; set; } = Guid.Empty;
		public int FrameId { get; set; } = 0;
		public DateTime DataCreatedAt { get; set; } = DateTime.MinValue;

		public int Width { get; set; } = 0;
		public int Height { get; set; } = 0;
		public string MimeType { get; set; } = "image/png";
		public string Base64Data { get; set; } = string.Empty;


		


		public ImageObjData()
		{
			// Parameterless constructor for serialization
		}

		[JsonConstructor]
		public ImageObjData(ImageObj? obj, int frameId = 0, string format = "png")
		{
			if (obj == null)
			{
				return;
			}

			if (frameId < 0 || frameId >= obj.FramesCount)
			{
				frameId = 0;
			}

			this.Id = obj.Id;
			this.FrameId = frameId;
			this.DataCreatedAt = DateTime.Now;

			this.Width = obj.Sizes.ElementAt(frameId).Width;
			this.Height = obj.Sizes.ElementAt(frameId).Height;

			if (!ImageCollection.SupportedFormats.Contains(format.ToLower()))
			{
				format = "png";
			}

			this.MimeType = "image/" + format;
			this.Base64Data = obj.GetBase64String(frameId, format) ?? string.Empty;
		}


	}
}
