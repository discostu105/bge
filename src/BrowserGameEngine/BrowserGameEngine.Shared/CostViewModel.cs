using BrowserGameEngine.GameDefinition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace BrowserGameEngine.Shared {
	public class CostViewModel {
		public IDictionary<string, decimal> Cost { get; init; }

		public static CostViewModel Create(Cost cost) {
			return new CostViewModel {
				Cost = cost.Resources.ToDictionary(x => x.Key.Id, y => y.Value)
			};
		}
	}
}



// workaround for roslyn bug: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
namespace System.Runtime.CompilerServices {
	public class IsExternalInit { }
}
