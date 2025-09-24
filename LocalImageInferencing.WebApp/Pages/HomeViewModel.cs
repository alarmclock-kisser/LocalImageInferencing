using LocalImageInferencing.Shared;
using Microsoft.AspNetCore.Components.Web;
using LocalImageInferencing.Client;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using Radzen;
using Radzen.Blazor;

namespace LocalImageInferencing.WebApp.Pages
{
    public class HomeViewModel
    {
        public List<ImageObjInfo> ImageList { get; set; } = new();
        public List<ImageListEntry> ImageListEntries { get; set; } = new();
        public Guid SelectedImageId { get; set; } = Guid.Empty;
        public ImageObjInfo? CurrentImageInfo => ImageList.FirstOrDefault(x => x.Id == SelectedImageId);
        public int CurrentFrame { get; set; } = 0;
        public string CurrentImageBase64 { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public string LlmResponse { get; set; } = string.Empty;
        public string[] DownloadFormats { get; set; } = new[] { "png", "jpg", "bmp" };
        public string SelectedFormat { get; set; } = "png";
        public bool UseAllFrames { get; set; } = false;
        public bool IsDarkMode { get; set; } = false;
        public bool UseThinking { get; set; } = false;
        public double? LastResponseSeconds;
        public string LastResponseText => LastResponseSeconds.HasValue ? $"{LastResponseSeconds.Value:F3} s" : "-";
        public string ImageResolutionText => CurrentImageInfo == null || CurrentImageInfo.Id == Guid.Empty ? "-" : _currentWidth > 0 ? $"{_currentWidth} x {_currentHeight}px" : "?";
        public string ImageDataSizeKBText => _lastFrameBytes > 0 ? $"{(_lastFrameBytes / 1024.0):F1} kB" : "-";
        private int _currentWidth = 0;
        private int _currentHeight = 0;
        private int _lastFrameBytes = 0;
        public int ImageWidth { get; set; } = 900;
        public int ImageHeight { get; set; } = (int)(900 * 9.0 / 16.0);
        public string ImageWidthPx => ImageWidth + "px";
        public string ImageHeightPx => ImageHeight + "px";
        public bool _resizeJsInitialized = false;
        public class ImageListEntry
        {
            public Guid Id { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }
        public void BuildImageListEntries()
        {
            ImageListEntries = ImageList.Select(i => new ImageListEntry
            {
                Id = i.Id,
                DisplayText = $"{(string.IsNullOrWhiteSpace(i.FilePath) ? "[Unbenannt]" : i.FilePath)} ({i.Id.ToString()[..8]})"
            }).ToList();
        }
        public void ResetFrameMeta()
        {
            _currentWidth = 0;
            _currentHeight = 0;
            _lastFrameBytes = 0;
        }
        public string CleanModelJson(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            var cleaned = input.Trim().Trim('`').Trim();
            int start = cleaned.IndexOf('{');
            int end = cleaned.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                cleaned = cleaned.Substring(start, end - start + 1);
            }
            return cleaned;
        }
        public class ImageResponseRectList
        {
            public Guid ImageId { get; set; }
            public int FrameId { get; set; }
            public IEnumerable<ImageResponseRect> Rectangles { get; set; } = new List<ImageResponseRect>();
        }
        public class ImageResponseRect
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float Confidence { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}
