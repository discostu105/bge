using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class CannotAffordException : Exception {
		public Cost? RequiredCost { get; }
		public Cost? AvailableResources { get; }

		public CannotAffordException() : base("Cannot afford the required cost.") {
		}

		public CannotAffordException(Cost cost) : base(BuildMessage(cost, null)) {
			RequiredCost = cost;
		}

		public CannotAffordException(Cost requiredCost, Cost availableResources) : base(BuildMessage(requiredCost, availableResources)) {
			RequiredCost = requiredCost;
			AvailableResources = availableResources;
		}

		public CannotAffordException(string? message) : base(message) {
		}

		public CannotAffordException(string? message, Exception? innerException) : base(message, innerException) {
		}

		// Build a message that names the resources actually short — so the UI can
		// surface "30/150 minerals" instead of the default "Exception of type ..."
		// fallback that .NET produces when no message is set.
		private static string BuildMessage(Cost required, Cost? available) {
			var requiredEntries = required.Resources.Where(r => r.Value > 0).ToList();
			if (requiredEntries.Count == 0) return "Cannot afford: no required resources specified.";

			if (available == null) {
				return "Cannot afford. Required: " + FormatEntries(requiredEntries) + ".";
			}

			var shortages = new List<string>();
			foreach (var (resourceId, requiredAmount) in requiredEntries) {
				available.Resources.TryGetValue(resourceId, out var have);
				if (have < requiredAmount) {
					shortages.Add($"{FormatAmount(have)}/{FormatAmount(requiredAmount)} {resourceId.Id}");
				}
			}

			if (shortages.Count == 0) {
				return "Cannot afford. Required: " + FormatEntries(requiredEntries) + ".";
			}

			return "Cannot afford. Short on: " + string.Join(", ", shortages) + ".";
		}

		private static string FormatEntries(IEnumerable<KeyValuePair<ResourceDefId, decimal>> entries) {
			return string.Join(", ", entries.Select(r => $"{FormatAmount(r.Value)} {r.Key.Id}"));
		}

		private static string FormatAmount(decimal value) {
			return value == Math.Truncate(value) ? value.ToString("0") : value.ToString("0.##");
		}
	}
}
