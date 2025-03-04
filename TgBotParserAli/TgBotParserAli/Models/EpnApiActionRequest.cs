using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBotParserAli.Models
{
    internal class EpnApiActionRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; } = "en";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = "USD";

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; } = 10;

        [JsonProperty("offset")]
        public int Offset { get; set; } = 0;

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("price_min")]
        public double PriceMin { get; set; } = 0.0;

        [JsonProperty("price_max")]
        public double PriceMax { get; set; } = 1000000.0;
    }
}
