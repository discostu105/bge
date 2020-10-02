using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BrowserGameEngine.BlazorClient.Auth {
	public class RedirectIfUnauthorizedHandler : DelegatingHandler {
		private readonly NavigationManager nav;

		public RedirectIfUnauthorizedHandler(NavigationManager nav) {
			this.nav = nav;
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
			var response = await base.SendAsync(request, cancellationToken);
			Console.WriteLine("{0}\t{1}\t{2}", request.RequestUri, (int)response.StatusCode, response.Headers.Date);
			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
				var returnUrl = nav.ToBaseRelativePath(nav.Uri);

				if (string.IsNullOrWhiteSpace(returnUrl)) {
					nav.NavigateTo("signin", true);
				} else {
					nav.NavigateTo($"signin?returnUrl={returnUrl}", true);
				}
			}
			return response;
		}
	}

}
