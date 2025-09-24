using LocalImageInferencing.Core;
using LocalImageInferencing.Shared;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;
using OllamaSharp.Models;
using System.Diagnostics;

namespace LocalImageInferencing.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class OllamaController : ControllerBase
	{
		private readonly OllamaApiClient ollamaClient;
		private readonly ImageCollection imageCollection;

		public Uri OllamaBaseUrl => this.ollamaClient.Uri;
		public string DefaultModelName { get; private set; } = "qwen2.5vl:7b";

		public Model? CurrentModel { get; private set; } = null;



		public OllamaController(OllamaApiConfig ollamaApiUrl, ImageCollection imageCollection)
		{
			this.ollamaClient = new OllamaApiClient(ollamaApiUrl.BaseUrl);
			this.DefaultModelName = ollamaApiUrl.DefaultModel;
			this.imageCollection = imageCollection;

			// Set default model on startup
			var models = this.ollamaClient.ListLocalModelsAsync().GetAwaiter().GetResult();
			this.CurrentModel = models?.FirstOrDefault(m => m.Name.Equals(this.DefaultModelName, StringComparison.OrdinalIgnoreCase));
			if (this.CurrentModel != null)
			{
				this.ollamaClient.SelectedModel = this.CurrentModel.Name;
				Console.WriteLine($"Default model set to: {this.CurrentModel.Name}");
			}
			else
			{
				Console.WriteLine($"Default model '{this.DefaultModelName}' not found in Ollama models.");
				this.CurrentModel = models?.FirstOrDefault(m => m.Name.Equals(this.ollamaClient.SelectedModel, StringComparison.OrdinalIgnoreCase));
			}
		}



		[HttpGet("status")]
		[ProducesResponseType(typeof(LlmStatusInfo), 200)]
		[ProducesResponseType(typeof(ProblemDetails), 400)]
		[ProducesResponseType(typeof(ProblemDetails), 500)]
		public async Task<ActionResult<LlmStatusInfo>> GetStatusAsync()
		{
			try
			{
				var models = await this.ollamaClient.ListLocalModelsAsync();
				var status = await Task.Run(() => new LlmStatusInfo(this.ollamaClient));
				if (status == null)
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "Failed to retrieve status from Ollama API."
					});
				}

				return this.Ok(status);
			}
			catch (Exception ex)
			{
				return this.StatusCode(500, new ProblemDetails
				{
					Status = 500,
					Title = "Internal Server Error",
					Detail = ex.Message
				});
			}
		}

		[HttpGet("models")]
		[ProducesResponseType(typeof(IEnumerable<string>), 200)]
		[ProducesResponseType(typeof(ProblemDetails), 500)]
		public async Task<ActionResult<IEnumerable<string>>> GetModelsAsync()
		{
			try
			{
				List<string> modelNames = [];

				var models = await this.ollamaClient.ListLocalModelsAsync();
				if (models != null)
				{
					foreach (var model in models)
					{
						if (!string.IsNullOrEmpty(model.Name))
						{
							modelNames.Add(model.Name);
						}
					}
				}

				return this.Ok(modelNames);
			}
			catch (Exception ex)
			{
				return this.StatusCode(500, new ProblemDetails
				{
					Status = 500,
					Title = "Internal Server Error",
					Detail = ex.Message
				});
			}
		}

		[HttpPost("setModel")]
		[ProducesResponseType(typeof(string), 200)]
		[ProducesResponseType(typeof(ProblemDetails), 400)]
		[ProducesResponseType(typeof(ProblemDetails), 500)]
		public async Task<ActionResult<string>> SetModelAsync([FromQuery] string modelName = "QWEN2_5VL_7B")
		{
			try
			{
				if (string.IsNullOrWhiteSpace(modelName))
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "Model name cannot be null or empty."
					});
				}

				var models = await this.ollamaClient.ListLocalModelsAsync();
				var model = models?.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
				if (model == null)
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = $"Model '{modelName}' not found."
					});
				}

				this.ollamaClient.SelectedModel = model.Name;
				this.CurrentModel = model;
				return this.Ok($"Model set to '{model.Name}'.");
			}
			catch (Exception ex)
			{
				return this.StatusCode(500, new ProblemDetails
				{
					Status = 500,
					Title = "Internal Server Error",
					Detail = ex.Message
				});
			}
		}

		[HttpGet("generateTextFromText")]
		[ProducesResponseType(typeof(LlmResponse), 200)]
		[ProducesResponseType(typeof(ProblemDetails), 400)]
		[ProducesResponseType(typeof(ProblemDetails), 500)]
		public async Task<ActionResult<LlmResponse>> GenerateTextFromTextAsync([FromQuery] string prompt)
		{
			Stopwatch sw = Stopwatch.StartNew();

			try
			{
				if (this.CurrentModel == null)
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "No model is currently set. Please set a model first."
					});
				}

				if (string.IsNullOrWhiteSpace(prompt))
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "Prompt cannot be null or empty."
					});
				}

				var generateRequest = new GenerateRequest
				{
					Model = this.CurrentModel.Name,
					Prompt = prompt
				};

				var responseStream = this.ollamaClient.GenerateAsync(generateRequest);
				string resultText = string.Empty;
				await foreach (var item in responseStream)
				{
					if (item != null && !string.IsNullOrEmpty(item.Response))
					{
						resultText += item.Response;
					}
				}

				var response = new LlmResponse
				{
					ResponseText = resultText,
					ResponseDelaySeconds = sw.Elapsed.TotalSeconds
				};

				return this.Ok(response);
			}
			catch (Exception ex)
			{
				return this.StatusCode(500, new ProblemDetails
				{
					Status = 500,
					Title = "Internal Server Error",
					Detail = ex.Message
				});
			}
			finally
			{
				sw.Stop();
				Console.WriteLine($"Text generation took {sw.Elapsed.TotalSeconds} seconds.");
			}
		}

		[HttpGet("generateTextFromImage")]
		[ProducesResponseType(typeof(LlmResponse), 200)]
		[ProducesResponseType(typeof(ProblemDetails), 400)]
		[ProducesResponseType(typeof(ProblemDetails), 404)]
		[ProducesResponseType(typeof(ProblemDetails), 500)]
		public async Task<ActionResult<LlmResponse>> GenerateTextFromImageAsync([FromQuery] string prompt, [FromQuery] Guid imageId, [FromQuery] int? frameId = null, string format = "png")
		{
			Stopwatch sw = Stopwatch.StartNew();
			try
			{
				if (this.CurrentModel == null)
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "No model is currently set. Please set a model first."
					});
				}

				if (string.IsNullOrWhiteSpace(prompt))
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "Prompt cannot be null or empty."
					});
				}

				var imageObj = this.imageCollection[imageId];
				if (imageObj == null)
				{
					return this.NotFound(new ProblemDetails
					{
						Status = 404,
						Title = "Not Found",
						Detail = $"Image with ID '{imageId}' not found."
					});
				}

				List<ImageObjData> imageDatas = [];
				if (frameId.HasValue)
				{
					imageDatas.Add(new ImageObjData(imageObj, frameId.Value, format));
				}
				else
				{
					for (int i = 0; i < imageObj.FramesCount; i++)
					{
						imageDatas.Add(new ImageObjData(imageObj, i, format));
					}
				}

				var generateRequest = new GenerateRequest
				{
					Model = this.CurrentModel.Name,
					Prompt = prompt,
					Images = imageDatas.Select(imgData => imgData.Base64Data).ToArray()
				};

				var responseStream = this.ollamaClient.GenerateAsync(generateRequest);
				
				string resultText = string.Empty;
				await foreach (var item in responseStream)
				{
					if (item != null && !string.IsNullOrEmpty(item.Response))
					{
						resultText += item.Response;
					}
				}

				var response = new LlmResponse
				{
					ResponseText = resultText,
					ResponseDelaySeconds = sw.Elapsed.TotalSeconds
				};

				if (response == null)
				{
					return this.BadRequest(new ProblemDetails
					{
						Status = 400,
						Title = "Bad Request",
						Detail = "Failed to generate response from Ollama API."
					});
				}

				return this.Ok(response);
			}
			catch (Exception ex)
			{
				return this.StatusCode(500, new ProblemDetails
				{
					Status = 500,
					Title = "Internal Server Error",
					Detail = ex.Message
				});
			}
			finally
			{
				sw.Stop();
				Console.WriteLine($"Image-based text generation took {sw.Elapsed.TotalSeconds} seconds.");
			}
		}




	}
}
