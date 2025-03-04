

using Newtonsoft.Json;

namespace TgBotParserAli.Models
{
    public class ProductOffer
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("id_category")]
        public int CategoryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("picture")]
        public string Picture { get; set; }

        [JsonProperty("all_images")]
        public List<string> AllImages { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("sale_price")]
        public decimal SalePrice { get; set; }

        [JsonProperty("ru_shipping_price")]
        public decimal? RuShippingPrice { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("product_id")]
        public long ProductId { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("commission_rate")]
        public decimal CommissionRate { get; set; }

        [JsonProperty("store_id")]
        public string StoreId { get; set; }

        [JsonProperty("store_title")]
        public string StoreTitle { get; set; }

        [JsonProperty("orders_count")]
        public int OrdersCount { get; set; }

        [JsonProperty("evaluatescore")]
        public string EvaluateScore { get; set; }

        [JsonProperty("prices")]
        public Dictionary<string, decimal> Prices { get; set; }

        [JsonProperty("sale_prices")]
        public Dictionary<string, decimal> SalePrices { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
        public string AddedAt { get; set; }
    }
}
