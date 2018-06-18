namespace pBrainTrain.App.Models
{
    using Newtonsoft.Json;

    public class Translations
    {
        [JsonProperty(PropertyName="es")]
        public string Spanish { get; set; }

        [JsonProperty(PropertyName = "fr")]
        public string French { get; set; }

        [JsonProperty(PropertyName = "ja")]
        public string Japanese { get; set; }

    }
}
