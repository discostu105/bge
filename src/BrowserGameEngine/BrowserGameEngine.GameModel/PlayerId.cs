using System;

namespace BrowserGameEngine.GameModel {
	public class PlayerId {
		public string Id { get; }
		public PlayerId(string id) { Id = id; }
		public bool Equals(PlayerId other) => Equals(Id, other.Id);
		public override bool Equals(object other) => (other as PlayerId)?.Equals(this) == true;
		public override int GetHashCode() => Id.GetHashCode();
		public override string ToString() => Id;

		public static bool operator ==(PlayerId obj1, PlayerId obj2) {
			if (ReferenceEquals(obj1, obj2)) return true;
			if (ReferenceEquals(obj1, null)) return false;
			if (ReferenceEquals(obj2, null)) return false;
			return (obj1.Equals(obj2));
		}

		public static bool operator !=(PlayerId obj1, PlayerId obj2) => !(obj1 == obj2);
		public static PlayerId Create(string playerId) => new PlayerId(playerId);
	}
}
