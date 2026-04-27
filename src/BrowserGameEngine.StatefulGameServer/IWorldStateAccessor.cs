using System;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public interface IWorldStateAccessor {
		WorldState WorldState { get; }

		/// <summary>
		/// Pushes an ambient <see cref="WorldState"/> that overrides the default
		/// resolution for the duration of the returned scope. Used by background
		/// services (e.g. <c>GameTickTimerService</c>) to direct singleton
		/// repositories at the correct per-game world while ticking. The scope
		/// flows with <see cref="System.Threading.AsyncLocal{T}"/> so awaits and
		/// continuations within the scope see the same override; disposing
		/// restores the previous value.
		/// </summary>
		IDisposable PushAmbient(WorldState worldState);
	}
}
