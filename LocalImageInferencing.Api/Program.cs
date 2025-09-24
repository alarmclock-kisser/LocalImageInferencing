using LocalImageInferencing.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LocalImageInferencing.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            bool swaggerEnabled = builder.Configuration.GetValue<bool>("SwaggerEnabled", true);
            int maxUploadSize = builder.Configuration.GetValue<int>("MaxUploadSizeMb", 256) * 1_000_000;
            string ollamaApiUrl = builder.Configuration.GetValue<string>("OllamaApiUrl") ?? "http://localhost:11434";
            string ollamaDefaultModel = builder.Configuration.GetValue<string>("OllamaDefaultModel") ?? "QWEN2_5VL_7B";

			// Add services to the container.
			builder.Services.AddSingleton<ImageCollection>();
            builder.Services.AddSingleton(new OllamaApiConfig(ollamaApiUrl, ollamaDefaultModel));

            // Swagger/OpenAPI (always register generator, conditionally expose UI later)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "LocalImageInferencing API",
                    Description = "API + WebApp using local-hosted OllamaApi for image analysis etc.",
                    TermsOfService = new Uri("https://localhost:7222/terms"),
                    Contact = new OpenApiContact { Name = "Junior Developer", Email = "marcel.king91299@gmail.com" }
                });
            });

            // Request Body Size Limits
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = maxUploadSize;
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = maxUploadSize;
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = maxUploadSize;
            });

            // Logging
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.Services.AddControllers();

            // CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("LIICors", policy =>
                {
                    policy.WithOrigins("https://localhost:7053")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Development-only Middlewares
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                if (swaggerEnabled)
                {
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalImageInferencing API v1");
                    });
                }
                else
                {
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalImageInferencing API Info Only");
                    });
                }
            }

            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseCors("LIICors");
            app.MapControllers();

            app.Run();
        }
    }

    public class OllamaApiConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string DefaultModel { get; set; } = "QWEN2_5VL_7B";

		public OllamaApiConfig()
        {
            // Empty constructor
        }

        public OllamaApiConfig(string baseUrl, string defaultModel)
        {
			this.BaseUrl = baseUrl;
            this.DefaultModel = defaultModel;
		}
    }
}
