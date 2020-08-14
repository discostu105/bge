using System.Web;

namespace BrowserGameEngine.GameDefinition {
	public class OAuthDef {
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string RedirectUrl { get; set; }
		public string UrlTemplate { get; set; }

		public string GetUrl() => UrlTemplate
			.Replace("{client_id}", HttpUtility.UrlEncode(ClientId))
			.Replace("{redirect_url}", HttpUtility.UrlEncode(RedirectUrl));
	}
}