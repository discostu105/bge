using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.GameDefinition.SCO {
	public static class RealisticDevScenario {
		private const int StartingTick = 150;
		private static readonly DateTime Now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

		private static readonly AllianceId IronWolvesId = new AllianceId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
		private static readonly AllianceId RedSunPactId = new AllianceId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
		private static readonly AllianceId AzureOrderId = new AllianceId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
		private static readonly AllianceId NomadGuildId = new AllianceId(Guid.Parse("44444444-4444-4444-4444-444444444444"));

		private static readonly HashSet<int> Elite = new() { 0, 5 };
		private static readonly HashSet<int> Strong = new() { 1, 2, 3, 6, 7, 8 };
		private static readonly HashSet<int> Average = new() { 4, 9, 10, 11, 12, 13, 14, 15 };

		private static readonly string[] Names = {
			"Ironclad", "Kovacs", "Tarkov", "Rourke", "Styre",
			"Crimson", "Vargas", "Zhao", "Renata", "Okonkwo",
			"Vashik", "Lenore", "Kiyomi", "Dren",
			"Dustrunner", "Calla", "Wayvern", "Orla",
			"Wanderer", "Stray"
		};

		public static WorldStateImmutable Build() {
			var gameTick = new GameTick(StartingTick);
			var players = Enumerable.Range(0, 20).Select(i => BuildPlayer(i, gameTick)).ToList();

			return new WorldStateImmutable(
				Players: players.ToDictionary(x => x.PlayerId),
				GameTickState: new GameTickStateImmutable(gameTick, Now),
				GameActionQueue: new List<GameActionImmutable>(),
				Alliances: BuildAlliances(),
				GameId: new GameId("default"),
				MarketOrders: BuildMarketOrders(),
				ChatMessages: BuildChatMessages(),
				Wars: BuildWars(),
				TradeOffers: BuildTradeOffers()
			);
		}

		private static PlayerId Pid(int i) => PlayerIdFactory.Create($"devstu#{i}");

		private static AllianceId? AllianceFor(int i) {
			if (i <= 4) return IronWolvesId;
			if (i <= 9) return RedSunPactId;
			if (i <= 13) return AzureOrderId;
			if (i <= 17) return NomadGuildId;
			return null;
		}

		private static PlayerImmutable BuildPlayer(int i, GameTick tick) {
			var resources = new Dictionary<ResourceDefId, decimal>();
			var assets = new HashSet<AssetImmutable>();
			var units = new List<UnitImmutable>();
			int mineralWorkers, gasWorkers;

			if (Elite.Contains(i)) {
				resources[Id.ResDef("minerals")] = 80_000m;
				resources[Id.ResDef("gas")] = 40_000m;
				resources[Id.ResDef("land")] = 120m;
				assets.Add(new AssetImmutable(Id.AssetDef("commandcenter"), 3));
				assets.Add(new AssetImmutable(Id.AssetDef("factory"), 3));
				assets.Add(new AssetImmutable(Id.AssetDef("armory"), 2));
				assets.Add(new AssetImmutable(Id.AssetDef("spaceport"), 2));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wbf"), 40, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("spacemarine"), 120, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 25, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wraith"), 15, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("battlecruiser"), 4, null));
				mineralWorkers = 20; gasWorkers = 12;
			} else if (Strong.Contains(i)) {
				resources[Id.ResDef("minerals")] = 40_000m;
				resources[Id.ResDef("gas")] = 20_000m;
				resources[Id.ResDef("land")] = 85m;
				assets.Add(new AssetImmutable(Id.AssetDef("commandcenter"), 2));
				assets.Add(new AssetImmutable(Id.AssetDef("factory"), 2));
				assets.Add(new AssetImmutable(Id.AssetDef("armory"), 1));
				assets.Add(new AssetImmutable(Id.AssetDef("spaceport"), 1));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wbf"), 25, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("spacemarine"), 70, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 10, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wraith"), 6, null));
				mineralWorkers = 14; gasWorkers = 8;
			} else if (Average.Contains(i)) {
				resources[Id.ResDef("minerals")] = 15_000m;
				resources[Id.ResDef("gas")] = 8_000m;
				resources[Id.ResDef("land")] = 55m;
				assets.Add(new AssetImmutable(Id.AssetDef("commandcenter"), 1));
				assets.Add(new AssetImmutable(Id.AssetDef("factory"), 1));
				assets.Add(new AssetImmutable(Id.AssetDef("armory"), 1));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wbf"), 15, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("spacemarine"), 35, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 3, null));
				mineralWorkers = 8; gasWorkers = 4;
			} else {
				resources[Id.ResDef("minerals")] = 3_000m;
				resources[Id.ResDef("gas")] = 1_000m;
				resources[Id.ResDef("land")] = 25m;
				assets.Add(new AssetImmutable(Id.AssetDef("commandcenter"), 1));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("wbf"), 8, null));
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("spacemarine"), 12, null));
				mineralWorkers = 4; gasWorkers = 2;
			}

			if (i == 0) units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 8, Pid(5), ReturnTimer: 4));
			if (i == 6) units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 5, Pid(2), ReturnTimer: 2));
			if (i == 1) units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("siegetank"), 6, Pid(9), ReturnTimer: 3));

			return new PlayerImmutable(
				PlayerId: Pid(i),
				PlayerType: Id.PlayerType("terran"),
				Name: $"Commander {Names[i]}",
				Created: Now - TimeSpan.FromHours(48),
				State: new PlayerStateImmutable(
					LastGameTickUpdate: Now,
					CurrentGameTick: tick,
					Resources: resources,
					Assets: assets,
					Units: units,
					MineralWorkers: mineralWorkers,
					GasWorkers: gasWorkers
				),
				LastOnline: Now - TimeSpan.FromMinutes(i * 3),
				AllianceId: AllianceFor(i)
			);
		}

		private static IDictionary<AllianceId, AllianceImmutable> BuildAlliances() {
			var dict = new Dictionary<AllianceId, AllianceImmutable>();

			dict[IronWolvesId] = new AllianceImmutable(
				AllianceId: IronWolvesId,
				Name: "Iron Wolves",
				PasswordHash: "",
				LeaderId: Pid(0),
				Created: Now - TimeSpan.FromDays(10),
				Members: Enumerable.Range(0, 5).Select(i => new AllianceMemberImmutable(
					PlayerId: Pid(i),
					IsPending: false,
					JoinedAt: Now - TimeSpan.FromDays(10 - i),
					VoteCount: 0
				)).ToList<AllianceMemberImmutable>(),
				Message: "To the last breath. The pact will fall.",
				Posts: BuildIronWolvesPosts(),
				ElectionHistory: new List<AllianceElectionImmutable> {
					new AllianceElectionImmutable(
						ElectionId: new AllianceElectionId(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")),
						AllianceId: IronWolvesId,
						Status: AllianceElectionStatus.Completed,
						StartedByPlayerId: Pid(1),
						StartedAt: Now - TimeSpan.FromDays(9),
						NominationEndsAt: Now - TimeSpan.FromDays(8),
						VotingEndsAt: Now - TimeSpan.FromDays(7),
						Candidates: new List<AllianceElectionCandidateImmutable> {
							new AllianceElectionCandidateImmutable(Pid(0), Now - TimeSpan.FromDays(9)),
							new AllianceElectionCandidateImmutable(Pid(2), Now - TimeSpan.FromDays(9))
						},
						Votes: new List<AllianceElectionVoteImmutable> {
							new AllianceElectionVoteImmutable(Pid(1), Pid(0), Now - TimeSpan.FromDays(8)),
							new AllianceElectionVoteImmutable(Pid(2), Pid(0), Now - TimeSpan.FromDays(8)),
							new AllianceElectionVoteImmutable(Pid(3), Pid(0), Now - TimeSpan.FromDays(8)),
							new AllianceElectionVoteImmutable(Pid(4), Pid(2), Now - TimeSpan.FromDays(8))
						},
						WinnerId: Pid(0),
						CompletedAt: Now - TimeSpan.FromDays(7)
					)
				}
			);

			dict[RedSunPactId] = new AllianceImmutable(
				AllianceId: RedSunPactId,
				Name: "Red Sun Pact",
				PasswordHash: "",
				LeaderId: Pid(5),
				Created: Now - TimeSpan.FromDays(9),
				Members: Enumerable.Range(5, 5).Select(i => new AllianceMemberImmutable(
					PlayerId: Pid(i),
					IsPending: false,
					JoinedAt: Now - TimeSpan.FromDays(9 - (i - 5)),
					VoteCount: 0
				)).ToList<AllianceMemberImmutable>(),
				Message: "Burn them where they stand.",
				Posts: BuildRedSunPosts()
			);

			dict[AzureOrderId] = new AllianceImmutable(
				AllianceId: AzureOrderId,
				Name: "Azure Order",
				PasswordHash: "",
				LeaderId: Pid(10),
				Created: Now - TimeSpan.FromDays(6),
				Members: Enumerable.Range(10, 4).Select(i => new AllianceMemberImmutable(
					PlayerId: Pid(i),
					IsPending: false,
					JoinedAt: Now - TimeSpan.FromDays(6 - (i - 10)),
					VoteCount: 0
				)).ToList<AllianceMemberImmutable>(),
				Message: "Neutrality is a blade, too.",
				Posts: new List<AlliancePostImmutable> {
					new AlliancePostImmutable(
						PostId: new AlliancePostId(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001")),
						AllianceId: AzureOrderId,
						AuthorPlayerId: Pid(11),
						Body: "I say we vote. Leadership has gone stale.",
						CreatedAt: Now - TimeSpan.FromHours(12)
					),
					new AlliancePostImmutable(
						PostId: new AlliancePostId(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002")),
						AllianceId: AzureOrderId,
						AuthorPlayerId: Pid(10),
						Body: "Fine. Cast your votes.",
						CreatedAt: Now - TimeSpan.FromHours(11)
					)
				},
				ActiveElection: new AllianceElectionImmutable(
					ElectionId: new AllianceElectionId(Guid.Parse("cccccccc-0000-0000-0000-000000000001")),
					AllianceId: AzureOrderId,
					Status: AllianceElectionStatus.Voting,
					StartedByPlayerId: Pid(11),
					StartedAt: Now - TimeSpan.FromHours(10),
					NominationEndsAt: Now - TimeSpan.FromHours(6),
					VotingEndsAt: Now + TimeSpan.FromHours(14),
					Candidates: new List<AllianceElectionCandidateImmutable> {
						new AllianceElectionCandidateImmutable(Pid(10), Now - TimeSpan.FromHours(9)),
						new AllianceElectionCandidateImmutable(Pid(11), Now - TimeSpan.FromHours(8))
					},
					Votes: new List<AllianceElectionVoteImmutable> {
						new AllianceElectionVoteImmutable(Pid(12), Pid(11), Now - TimeSpan.FromHours(4)),
						new AllianceElectionVoteImmutable(Pid(13), Pid(10), Now - TimeSpan.FromHours(3))
					}
				)
			);

			dict[NomadGuildId] = new AllianceImmutable(
				AllianceId: NomadGuildId,
				Name: "Nomad Guild",
				PasswordHash: "",
				LeaderId: Pid(14),
				Created: Now - TimeSpan.FromDays(5),
				Members: Enumerable.Range(14, 4).Select(i => new AllianceMemberImmutable(
					PlayerId: Pid(i),
					IsPending: false,
					JoinedAt: Now - TimeSpan.FromDays(5 - (i - 14)),
					VoteCount: 0
				)).ToList<AllianceMemberImmutable>(),
				Message: "The dunes remember.",
				Posts: new List<AlliancePostImmutable> {
					new AlliancePostImmutable(
						PostId: new AlliancePostId(Guid.Parse("dddddddd-0000-0000-0000-000000000001")),
						AllianceId: NomadGuildId,
						AuthorPlayerId: Pid(14),
						Body: "Stay low. Let the big houses bleed each other.",
						CreatedAt: Now - TimeSpan.FromDays(2)
					),
					new AlliancePostImmutable(
						PostId: new AlliancePostId(Guid.Parse("dddddddd-0000-0000-0000-000000000002")),
						AllianceId: NomadGuildId,
						AuthorPlayerId: Pid(15),
						Body: "Invited the Wanderer. Could use another set of hands.",
						CreatedAt: Now - TimeSpan.FromHours(20)
					)
				},
				Invites: new List<AllianceInviteImmutable> {
					new AllianceInviteImmutable(
						InviteId: new AllianceInviteId(Guid.Parse("eeeeeeee-0000-0000-0000-000000000001")),
						AllianceId: NomadGuildId,
						InviterPlayerId: Pid(15),
						InviteePlayerId: Pid(18),
						CreatedAt: Now - TimeSpan.FromHours(20),
						ExpiresAt: Now + TimeSpan.FromDays(2)
					)
				}
			);

			return dict;
		}

		private static IList<AlliancePostImmutable> BuildIronWolvesPosts() {
			var rows = new List<(int author, string body, int hoursAgo)> {
				(0, "Scouts report RSP massing near the border. Stay sharp.", 36),
				(1, "Tank columns ready. Give the word.", 32),
				(2, "We hit them at the next dawn tick.", 28),
				(3, "Factory output is up. Armory upgrade queued.", 24),
				(0, "Kovacs, take point on the left flank.", 20),
				(4, "Need more minerals. Anyone spare 5k?", 16),
				(1, "Sent 5k minerals to Styre. We stand together.", 12),
				(0, "First blood is ours. Press the advantage.", 4)
			};
			return rows.Select((p, idx) => new AlliancePostImmutable(
				PostId: new AlliancePostId(Guid.Parse($"ffffffff-0000-0000-0000-{idx:D12}")),
				AllianceId: IronWolvesId,
				AuthorPlayerId: Pid(p.author),
				Body: p.body,
				CreatedAt: Now - TimeSpan.FromHours(p.hoursAgo)
			)).ToList<AlliancePostImmutable>();
		}

		private static IList<AlliancePostImmutable> BuildRedSunPosts() {
			var rows = new List<(int author, string body, int hoursAgo)> {
				(5, "The Wolves are slow. We strike first.", 40),
				(6, "Vargas here. Wraiths fueled and ready.", 30),
				(7, "Losing ground on the east flank. Need support.", 18),
				(5, "Hold the line. Reinforcements inbound.", 12),
				(8, "Siege tanks rolling toward Kovacs now.", 2)
			};
			return rows.Select((p, idx) => new AlliancePostImmutable(
				PostId: new AlliancePostId(Guid.Parse($"ffffffff-1111-0000-0000-{idx:D12}")),
				AllianceId: RedSunPactId,
				AuthorPlayerId: Pid(p.author),
				Body: p.body,
				CreatedAt: Now - TimeSpan.FromHours(p.hoursAgo)
			)).ToList<AlliancePostImmutable>();
		}

		private static IDictionary<AllianceWarId, AllianceWarImmutable> BuildWars() {
			var warId = new AllianceWarId(Guid.Parse("99999999-0000-0000-0000-000000000001"));
			return new Dictionary<AllianceWarId, AllianceWarImmutable> {
				{ warId, new AllianceWarImmutable(
					WarId: warId,
					AttackerAllianceId: IronWolvesId,
					DefenderAllianceId: RedSunPactId,
					Status: AllianceWarStatus.Active,
					DeclaredAt: Now - TimeSpan.FromHours(40),
					ProposerAllianceId: IronWolvesId
				) }
			};
		}

		private static IList<ChatMessageImmutable> BuildChatMessages() {
			var rows = new List<(int author, string body, int minutesAgo)> {
				(0, "Wolves rise.", 600),
				(5, "Pact answers.", 590),
				(10, "Keep us out of this.", 520),
				(14, "Wars are bad for trade.", 500),
				(18, "Anyone have minerals for sale?", 480),
				(1, "@Crimson, your tanks were sloppy.", 420),
				(5, "@Kovacs, yours never arrived.", 415),
				(11, "Peaceful resolution is still possible.", 360),
				(0, "Not today, Vashik.", 355),
				(6, "Wraiths sighted over the Wolves' capital.", 240),
				(2, "AA guns online. Come and try.", 235),
				(15, "Nomads trade salt for news.", 180),
				(7, "RSP needs medics, any volunteers?", 120),
				(4, "Styre here, standing by with the armory.", 60),
				(5, "We end this at tick 200.", 10)
			};
			return rows.Select((m, idx) => new ChatMessageImmutable(
				MessageId: new ChatMessageId(Guid.Parse($"77777777-0000-0000-0000-{idx:D12}")),
				AuthorPlayerId: Pid(m.author),
				Body: m.body,
				CreatedAt: Now - TimeSpan.FromMinutes(m.minutesAgo)
			)).ToList<ChatMessageImmutable>();
		}

		private static IList<MarketOrderImmutable> BuildMarketOrders() {
			var rows = new List<(int seller, string offered, decimal offeredAmt, string wanted, decimal wantedAmt, int hoursAgo)> {
				(0, "minerals", 5000m, "gas", 2500m, 8),
				(5, "gas", 3000m, "minerals", 6000m, 6),
				(3, "minerals", 2000m, "gas", 1000m, 5),
				(11, "gas", 1500m, "minerals", 3200m, 4),
				(14, "minerals", 4000m, "gas", 2100m, 3),
				(18, "gas", 500m, "minerals", 1200m, 1)
			};
			return rows.Select((o, idx) => new MarketOrderImmutable(
				OrderId: new MarketOrderId(Guid.Parse($"66666666-0000-0000-0000-{idx:D12}")),
				SellerPlayerId: Pid(o.seller),
				OfferedResourceId: Id.ResDef(o.offered),
				OfferedAmount: o.offeredAmt,
				WantedResourceId: Id.ResDef(o.wanted),
				WantedAmount: o.wantedAmt,
				CreatedAt: Now - TimeSpan.FromHours(o.hoursAgo),
				Status: MarketOrderStatus.Open
			)).ToList<MarketOrderImmutable>();
		}

		private static IList<TradeOfferImmutable> BuildTradeOffers() {
			var rows = new List<(int from, int to, string offered, decimal offeredAmt, string wanted, decimal wantedAmt, string note, int hoursAgo)> {
				(0, 4, "minerals", 5000m, "gas", 2000m, "Stock up, Styre.", 3),
				(5, 7, "gas", 1500m, "minerals", 3000m, "For the front lines.", 2),
				(10, 13, "minerals", 1000m, "gas", 500m, "Medical supplies.", 1)
			};
			return rows.Select((o, idx) => new TradeOfferImmutable(
				OfferId: new TradeOfferId(Guid.Parse($"55555555-0000-0000-0000-{idx:D12}")),
				FromPlayerId: Pid(o.from),
				ToPlayerId: Pid(o.to),
				OfferedResourceId: Id.ResDef(o.offered),
				OfferedAmount: o.offeredAmt,
				WantedResourceId: Id.ResDef(o.wanted),
				WantedAmount: o.wantedAmt,
				Note: o.note,
				CreatedAt: Now - TimeSpan.FromHours(o.hoursAgo),
				Status: TradeOfferStatus.Pending
			)).ToList<TradeOfferImmutable>();
		}
	}
}
