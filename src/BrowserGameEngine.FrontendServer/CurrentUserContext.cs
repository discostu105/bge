using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class CurrentUserContext {
		public PlayerId PlayerId { get; set; }

		public static CurrentUserContext Create(string playerId) {
			return new CurrentUserContext {
				PlayerId = PlayerIdFactory.Create(playerId)
			};
		}
	}
}
