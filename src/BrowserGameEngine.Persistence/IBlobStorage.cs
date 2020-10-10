using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.Persistence {
	public interface IBlobStorage {
		Task Store(string name, byte[] blob);
		Task<byte[]> Load(string name);
		bool Exists(string name);
	}
}
