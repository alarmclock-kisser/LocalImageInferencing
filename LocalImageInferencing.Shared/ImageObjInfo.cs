using LocalImageInferencing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalImageInferencing.Shared
{
    public class SizeInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ImageObjInfo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.MinValue;
        public string FilePath { get; set; } = string.Empty;

        public IEnumerable<SizeInfo> Sizes { get; set; } = [];
        public int FramesCount { get; set; } = 0;
        public int Channels { get; set; } = 4;
        public int BitsPerChannel { get; set; } = 8;
        public int BitsPerPixel { get; set; } = 32;

        public IEnumerable<float> FrameSizesMb { get; set; } = [];
        public IEnumerable<float> FrameBase64SizesMb { get; set; } = [];
        public float ScalingFactor { get; set; } = 1.0f;

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
            this.Sizes = obj.Sizes.Select(s => new SizeInfo { Width = s.Width, Height = s.Height });
            this.FramesCount = obj.FramesCount;
            this.Channels = obj.Channels;
            this.BitsPerChannel = obj.BitsPerChannel;
            this.BitsPerPixel = obj.BitsPerPixel;
            this.FrameSizesMb = obj.FrameSizesMb;
            this.FrameBase64SizesMb = obj.FrameBase64SizesMb;
            this.ScalingFactor = obj.ScalingFactor;
			this.OnHost = obj.OnHost;
            this.OnDevice = obj.OnDevice;
            this.Pointer = obj.Pointer.ToString();
        }
    }
}
