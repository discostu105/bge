namespace BrowserGameEngine.StatefulGameServer.Events;

/// <summary>Well-known SignalR event method names pushed to clients.</summary>
public static class GameEventTypes
{
	public const string ReceiveNotification = "ReceiveNotification";
	public const string ReceiveAlert = "ReceiveAlert";
	public const string ReceiveChatMessage = "ReceiveChatMessage";
	public const string ReceiveAllianceChatMessage = "ReceiveAllianceChatMessage";
	public const string GameTickCompleted = "GameTickCompleted";
	public const string MarketOrderFilled = "MarketOrderFilled";
	public const string GameFinalized = "GameFinalized";
}
