using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BrowserGameEngine.BlazorClient {
    public class CustomUserAccount : RemoteUserAccount {
        [JsonPropertyName("amr")]
        public string[] AuthenticationMethod { get; set; }
    }
}
