using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LocalImageInferencing.Shared
{
    public class LlmResponse
    {
        public Guid ResponseId { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ResponseText { get; set; } = string.Empty;
        public double ResponseDelaySeconds { get; set; } = 0.0;
        public bool DeeplyThought { get; set; } = false;



        public LlmResponse()
        {
            // Empty ctor for serialization
        }

        [JsonConstructor]
        public LlmResponse(string? text, double delay = 0.0, bool thinking = false)
        {
            this.ResponseText = text ?? string.Empty;
            this.ResponseDelaySeconds = delay;
            this.DeeplyThought = thinking;
        }




    }
}
