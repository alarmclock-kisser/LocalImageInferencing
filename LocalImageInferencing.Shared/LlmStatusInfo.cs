using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalImageInferencing.Shared
{
	public class LlmStatusInfo
	{
		public DateTime InfoCreatedAt { get; set; } = DateTime.UtcNow;
		public bool Online { get; set; } = false;
		public string? BaseUrl { get; set; } = null;
		public string? ModelName { get; set; } = null;
		public string? ModelDetails { get; set; } = null;
		public string ModelSizeBytes { get; set; } = "0";
		public IEnumerable<string> AvailableModels { get; set; } = [];
		public string? ErrorMessage { get; set; } = null;

		public LlmStatusInfo()
		{
			// Empty constructor
		}

		[JsonConstructor]
		public LlmStatusInfo(OllamaApiClient? ollamaApiClient)
		{
			if (ollamaApiClient == null)
			{
				this.Online = false;
				this.ErrorMessage = "OllamaApiClient is null.";
				return;
			}

			try
			{
				var modelsTask = ollamaApiClient.ListLocalModelsAsync();
				modelsTask.Wait();
				var models = modelsTask.Result;
				this.Online = true;
				this.BaseUrl = ollamaApiClient.Uri.ToString();
				this.AvailableModels = models?.Select(m => m.Name).ToList() ?? [];
				if (models != null && models.Any())
				{
					string selectedModelName = ollamaApiClient.SelectedModel;
					var defaultModel = models.FirstOrDefault(m => m.Name.Equals(selectedModelName, StringComparison.OrdinalIgnoreCase)) ?? models.First();
					this.ModelName = defaultModel.Name;
					this.ModelDetails = defaultModel.Details.ToString();
					this.ModelSizeBytes = defaultModel.Size.ToString();
				}
				else
				{
					this.ModelName = "None";
					this.ModelDetails = "N/A";
					this.ModelSizeBytes = "0";
				}
			}
			catch (Exception ex)
			{
				this.Online = false;
				this.ErrorMessage = ex.Message;
			}
		}
	}
}
