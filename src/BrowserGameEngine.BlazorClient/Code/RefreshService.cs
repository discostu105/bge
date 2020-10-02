using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.BlazorClient.Code {

	public class RefreshService {
		public event Action RefreshRequested;
		public void CallRequestRefresh() {
			RefreshRequested?.Invoke();
		}
	}
}
