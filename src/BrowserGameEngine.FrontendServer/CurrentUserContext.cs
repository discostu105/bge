using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class CurrentUserContext {
		public PlayerId? PlayerId { get; set; }

		public bool IsValid { get; set; } = false;

		public static CurrentUserContext Create(string playerId) {
			return new CurrentUserContext {
				PlayerId = PlayerIdFactory.Create(playerId)
			};
		}

		public static CurrentUserContext Inactive() {
			return new CurrentUserContext {
				PlayerId = null,
				IsValid = false
			};
		}

		public void Activate(PlayerId playerId) {
			this.PlayerId = playerId;
			this.IsValid = true;
		}

		public void DeActivate() {
			this.PlayerId = null;
			this.IsValid = false;
		}
	}
}
