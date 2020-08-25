using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class CurrentUserContext {
		public PlayerId PlayerId { get; set; }
		public string PlayerTypeId { get; set; }

		public static CurrentUserContext Create(string playerId, string playerTypeId) {
			return new CurrentUserContext {
				PlayerId = PlayerIdFactory.Create(playerId),
				PlayerTypeId = playerTypeId
			};
		}
	}
}
