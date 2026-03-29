using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrowserGameEngine.GameModel {
	public record AllianceId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class AllianceIdFactory {
		public static AllianceId Create(Guid id) => new AllianceId(id);
		public static AllianceId Create(string id) => new AllianceId(Guid.Parse(id));
		public static AllianceId NewAllianceId() => new AllianceId(Guid.NewGuid());
	}
}
