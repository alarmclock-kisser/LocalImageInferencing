using LocalImageInferencing.Shared;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalImageInferencing.Client
{
	public class ApiClient
	{
		private readonly InternalClient internalClient;
		private readonly HttpClient httpClient;
		private readonly string baseUrl;

		public string BaseUrl => this.baseUrl;



		public ApiClient(HttpClient httpClient)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			this.baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? throw new InvalidOperationException("HttpClient.BaseAddress is not set. Configure it in DI registration.");
			this.internalClient = new InternalClient(this.baseUrl, this.httpClient);
		}


		public async Task<IEnumerable<ImageObjInfo>> GetImageListAsync()
		{
			try
			{
				return (await this.internalClient.ListAsync()).ToList();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return [];
			}
		}

		public async Task<ImageObjInfo> UploadImageAsync(FileParameter file)
		{
			try
			{
				return await this.internalClient.LoadAsync(file);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return new ImageObjInfo();
			}
		}

		public async Task<ImageObjInfo> UploadImageAsync(IBrowserFile browserFile)
		{
			try
			{
				using var content = new MultipartFormDataContent();
				await using var stream = browserFile.OpenReadStream(long.MaxValue);
				var sc = new StreamContent(stream);
				sc.Headers.ContentType = new MediaTypeHeaderValue(browserFile.ContentType ?? "application/octet-stream");
				content.Add(sc, "file", browserFile.Name);
				var response = await this.httpClient.PostAsync("api/image/load", content);
				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine(await response.Content.ReadAsStringAsync());
					return new ImageObjInfo();
				}
				var json = await response.Content.ReadAsStringAsync();
				return JsonSerializer.Deserialize<ImageObjInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ImageObjInfo();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Upload exception: " + ex);
				return new ImageObjInfo();
			}
		}


		public async Task RemoveImageAsync(Guid id)
		{
			try
			{
				await this.internalClient.RemoveAsync(id);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public async Task ClearImagesAsync()
		{
			try
			{
				await this.internalClient.ClearAllAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}


		public async Task<ImageObjData> GetImageData(Guid id, int frameId = 0, string format = "png")
		{
			try
			{
				return await this.internalClient.DataAsync(id, frameId, format);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return new ImageObjData();
			}
		}

		public async Task<FileResponse?> DownloadImageAsync(Guid id, int frameId = 0, string format = "png")
		{
			try
			{
				return await this.internalClient.DownloadAsync(id, frameId, format);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return null;
			}
		}


		public async Task<LlmResponse> GenerateTextAsync(string prompt)
		{
			try
			{
				return await this.internalClient.GenerateTextFromTextAsync(prompt);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return new LlmResponse();
			}
		}

		public async Task<LlmResponse> GenerateTextFromImageAsync(string prompt, Guid imageId, int? frameId = 0, string format = "png")
		{
			try
			{
				return await this.internalClient.GenerateTextFromImageAsync(prompt, imageId, frameId, format);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return new LlmResponse();
			}
		}
	}
}
