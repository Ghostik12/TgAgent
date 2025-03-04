using Newtonsoft.Json;

namespace TgBotParserAli.Models
{
    internal class EpnApiRequest
    {
        [JsonProperty("user_api_key")]
        public string UserApiKey { get; set; }

        [JsonProperty("user_hash")]
        public string UserHash { get; set; }

        [JsonProperty("api_version")]
        public string ApiVersion { get; set; } = "2";

        [JsonProperty("requests")]
        public Dictionary<string, EpnApiActionRequest> Requests { get; set; }
    }
}
