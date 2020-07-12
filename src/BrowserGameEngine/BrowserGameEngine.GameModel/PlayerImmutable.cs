using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class PlayerImmutable {
		public PlayerId PlayerId { get; set; }
		public string Name { get; set; }
		public DateTime Created { get; set; }
		public PlayerStateImmutable State { get; set; }
	}
}
