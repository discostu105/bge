"""Tests for AllianceStrategyModule."""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass, field
from unittest.mock import MagicMock

from agent1.state.alliance_tracker import AllianceTracker
from agent1.strategy.alliance_module import AllianceAction, AllianceStrategyModule


def _make_tracker(
	*,
	my_alliance_id: str | None = "a1",
	allied_player_ids: set | None = None,
	war_enemy_player_ids: set | None = None,
	known_alliances: dict | None = None,
) -> AllianceTracker:
	return AllianceTracker(
		my_alliance_id=my_alliance_id,
		allied_player_ids=allied_player_ids or set(),
		war_enemy_player_ids=war_enemy_player_ids or set(),
		known_alliances=known_alliances or {},
	)


def _make_state(
	*,
	attackable_players: list[dict] | None = None,
	units: int = 10,
) -> MagicMock:
	state = MagicMock()
	state.attackable_players = attackable_players or []
	state.units = units
	return state


def _make_config(*, alliance_mode: str = "passive", overrides: dict | None = None) -> MagicMock:
	cfg = MagicMock()
	cfg.alliance_mode = alliance_mode
	cfg.strategy_overrides = overrides or {}
	return cfg


def _make_ctx(
	*,
	tracker: AllianceTracker | None = None,
	attackable_players: list[dict] | None = None,
	units: int = 10,
	alliance_mode: str = "passive",
	overrides: dict | None = None,
) -> MagicMock:
	ctx = MagicMock()
	ctx.alliance_tracker = tracker
	ctx.state = _make_state(attackable_players=attackable_players, units=units)
	ctx.config = _make_config(alliance_mode=alliance_mode, overrides=overrides)
	ctx.attack_target_scores = {}
	return ctx


class TestScoreAttackTargets:
	def test_no_tracker_all_enemies_get_default_score(self) -> None:
		# Without an alliance tracker every player is treated as neutral.
		module = AllianceStrategyModule()
		ctx = _make_ctx(tracker=None, attackable_players=[{"playerId": "p1"}])
		scores = module.score_attack_targets(ctx)
		assert scores == {"p1": 1.0}

	def test_neutral_enemy_gets_default_score(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(allied_player_ids=set(), war_enemy_player_ids=set())
		ctx = _make_ctx(tracker=tracker, attackable_players=[{"playerId": "neutral"}])
		scores = module.score_attack_targets(ctx)
		assert scores["neutral"] == 1.0

	def test_allied_player_gets_zero_score(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(allied_player_ids={"ally1"})
		ctx = _make_ctx(tracker=tracker, attackable_players=[{"playerId": "ally1"}])
		scores = module.score_attack_targets(ctx)
		assert scores["ally1"] == 0.0

	def test_war_enemy_gets_boosted_score(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(war_enemy_player_ids={"enemy1"})
		ctx = _make_ctx(tracker=tracker, attackable_players=[{"playerId": "enemy1"}])
		scores = module.score_attack_targets(ctx)
		assert scores["enemy1"] == 2.0

	def test_war_enemy_scores_higher_than_neutral(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(war_enemy_player_ids={"enemy1"})
		ctx = _make_ctx(
			tracker=tracker,
			attackable_players=[
				{"playerId": "neutral"},
				{"playerId": "enemy1"},
			],
		)
		scores = module.score_attack_targets(ctx)
		assert scores["enemy1"] > scores["neutral"]

	def test_allied_score_lower_than_neutral(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(allied_player_ids={"ally1"})
		ctx = _make_ctx(
			tracker=tracker,
			attackable_players=[
				{"playerId": "neutral"},
				{"playerId": "ally1"},
			],
		)
		scores = module.score_attack_targets(ctx)
		assert scores["ally1"] < scores["neutral"]

	def test_missing_player_id_skipped(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker()
		ctx = _make_ctx(tracker=tracker, attackable_players=[{}])
		scores = module.score_attack_targets(ctx)
		assert scores == {}

	def test_mixed_targets(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(
			allied_player_ids={"ally1"},
			war_enemy_player_ids={"war_enemy"},
		)
		ctx = _make_ctx(
			tracker=tracker,
			attackable_players=[
				{"playerId": "ally1"},
				{"playerId": "neutral"},
				{"playerId": "war_enemy"},
			],
		)
		scores = module.score_attack_targets(ctx)
		assert scores["ally1"] == 0.0
		assert scores["neutral"] == 1.0
		assert scores["war_enemy"] == 2.0


class TestDecideAllianceActionPassiveMode:
	def test_passive_mode_returns_empty(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(my_alliance_id=None)
		ctx = _make_ctx(tracker=tracker, alliance_mode="passive")
		actions = module.decide_alliance_action(ctx)
		assert actions == []

	def test_passive_mode_no_alliance_still_empty(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(my_alliance_id=None)
		ctx = _make_ctx(tracker=tracker, alliance_mode="passive")
		actions = module.decide_alliance_action(ctx)
		assert actions == []

	def test_none_tracker_returns_empty_in_any_mode(self) -> None:
		module = AllianceStrategyModule()
		ctx = _make_ctx(tracker=None, alliance_mode="active")
		actions = module.decide_alliance_action(ctx)
		assert actions == []


class TestDecideAllianceActionActiveMode:
	def test_creates_alliance_when_not_member(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(my_alliance_id=None)
		ctx = _make_ctx(
			tracker=tracker,
			alliance_mode="active",
			overrides={"alliance_name": "MyBot", "alliance_password": "secret"},
		)
		actions = module.decide_alliance_action(ctx)
		assert len(actions) == 1
		assert actions[0].kind == "create"
		assert actions[0].kwargs["name"] == "MyBot"
		assert actions[0].kwargs["password"] == "secret"

	def test_create_uses_default_name_when_not_configured(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(my_alliance_id=None)
		ctx = _make_ctx(tracker=tracker, alliance_mode="active")
		actions = module.decide_alliance_action(ctx)
		assert actions[0].kind == "create"
		assert actions[0].kwargs["name"] == "BotAlliance"

	def test_no_action_when_already_member_and_no_war(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(
			my_alliance_id="a1",
			war_enemy_player_ids=set(),
			known_alliances={},
		)
		ctx = _make_ctx(tracker=tracker, alliance_mode="active", units=10)
		actions = module.decide_alliance_action(ctx)
		assert actions == []

	def test_declares_war_when_above_threshold(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(
			my_alliance_id="a1",
			war_enemy_player_ids=set(),
			known_alliances={
				"a1": {"allianceId": "a1", "memberCount": 2},
				"enemy-a": {"allianceId": "enemy-a", "memberCount": 5},
			},
		)
		ctx = _make_ctx(
			tracker=tracker,
			alliance_mode="active",
			units=100,
			overrides={"war_unit_threshold": 50},
		)
		actions = module.decide_alliance_action(ctx)
		assert len(actions) == 1
		assert actions[0].kind == "declare_war"
		assert actions[0].kwargs["my_alliance_id"] == "a1"
		assert actions[0].kwargs["target_alliance_id"] == "enemy-a"

	def test_no_war_when_below_threshold(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(
			my_alliance_id="a1",
			war_enemy_player_ids=set(),
			known_alliances={"enemy-a": {"allianceId": "enemy-a", "memberCount": 5}},
		)
		ctx = _make_ctx(
			tracker=tracker,
			alliance_mode="active",
			units=10,
			overrides={"war_unit_threshold": 50},
		)
		actions = module.decide_alliance_action(ctx)
		assert actions == []

	def test_no_war_when_already_at_war(self) -> None:
		module = AllianceStrategyModule()
		tracker = _make_tracker(
			my_alliance_id="a1",
			war_enemy_player_ids={"e1"},  # already at war
			known_alliances={"enemy-a": {"allianceId": "enemy-a", "memberCount": 5}},
		)
		ctx = _make_ctx(
			tracker=tracker,
			alliance_mode="active",
			units=100,
			overrides={"war_unit_threshold": 50},
		)
		actions = module.decide_alliance_action(ctx)
		# war_enemy_player_ids is non-empty → skip war declaration
		assert not any(a.kind == "declare_war" for a in actions)
