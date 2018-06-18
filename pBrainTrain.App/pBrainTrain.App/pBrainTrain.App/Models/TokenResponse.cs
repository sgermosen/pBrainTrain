namespace pBrainTrain.App.Models
{
    using System;
    using Newtonsoft.Json;

    public class TokenResponse
    {
        #region Properties
        [JsonProperty(PropertyName = "access_token")] //the way (name)  how its come from the api
        public string AccessToken { get; set; } //the way (name) how im going to call it (use it) on my app

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = ".issued")]
        public DateTime Issued { get; set; }

        [JsonProperty(PropertyName = ".expires")]
        public DateTime Expires { get; set; }

        [JsonProperty(PropertyName = "error_description")]
        public string ErrorDescription { get; set; }
        #endregion
    }
}
