"""Tests for AllianceTracker."""
from __future__ import annotations

from unittest.mock import MagicMock

from agent1.state.alliance_tracker import AllianceTracker


def _make_client(
	*,
	is_member: bool = True,
	alliance_id: str = "a1",
	member_player_ids: list[str] | None = None,
	all_alliances: list[dict] | None = None,
	wars: list[dict] | None = None,
	enemy_member_ids: list[str] | None = None,
) -> MagicMock:
	"""Return a mock BgeClient configured for the given scenario."""
	client = MagicMock()

	client.get_my_alliance_status.return_value = {
		"isMember": is_member,
		"allianceId": alliance_id if is_member else None,
		"allianceName": "Test Alliance",
	}

	client.get_all_alliances.return_value = all_alliances or [
		{"allianceId": "a1", "name": "Test Alliance", "memberCount": 2},
	]

	members = [{"playerId": pid, "isPending": False} for pid in (member_player_ids or ["p1", "p2"])]
	enemy_members = [{"playerId": pid, "isPending": False} for pid in (enemy_member_ids or ["e1", "e2"])]

	def get_alliance_detail(aid: str) -> dict:
		if aid == alliance_id:
			return {"allianceId": aid, "members": members}
		return {"allianceId": aid, "members": enemy_members}

	client.get_alliance_detail.side_effect = get_alliance_detail
	client.get_alliance_wars.return_value = wars or []
	return client


class TestAllianceTrackerNotMember:
	def test_not_member_clears_state(self) -> None:
		tracker = AllianceTracker(
			my_alliance_id="old",
			allied_player_ids={"p1"},
			war_enemy_player_ids={"e1"},
		)
		client = _make_client(is_member=False)
		tracker.update(client)

		assert tracker.my_alliance_id is None
		assert tracker.allied_player_ids == set()
		assert tracker.war_enemy_player_ids == set()

	def test_not_member_does_not_call_alliance_apis(self) -> None:
		tracker = AllianceTracker()
		client = _make_client(is_member=False)
		tracker.update(client)

		client.get_alliance_detail.assert_not_called()
		client.get_alliance_wars.assert_not_called()


class TestAllianceTrackerMember:
	def test_my_alliance_id_set(self) -> None:
		tracker = AllianceTracker()
		client = _make_client(alliance_id="alliance-42", member_player_ids=["p1"])
		tracker.update(client)

		assert tracker.my_alliance_id == "alliance-42"

	def test_allied_player_ids_populated(self) -> None:
		tracker = AllianceTracker()
		client = _make_client(member_player_ids=["p1", "p2", "p3"])
		tracker.update(client)

		assert tracker.allied_player_ids == {"p1", "p2", "p3"}

	def test_pending_members_excluded(self) -> None:
		tracker = AllianceTracker()
		client = _make_client()
		client.get_alliance_detail.side_effect = None
		client.get_alliance_detail.return_value = {
			"allianceId": "a1",
			"members": [
				{"playerId": "p1", "isPending": False},
				{"playerId": "p2", "isPending": True},
			],
		}
		tracker.update(client)

		assert "p1" in tracker.allied_player_ids
		assert "p2" not in tracker.allied_player_ids

	def test_is_ally_returns_true_for_member(self) -> None:
		tracker = AllianceTracker()
		client = _make_client(member_player_ids=["p1", "p2"])
		tracker.update(client)

		assert tracker.is_ally("p1") is True
		assert tracker.is_ally("stranger") is False

	def test_no_wars_means_empty_war_enemies(self) -> None:
		tracker = AllianceTracker()
		client = _make_client(wars=[])
		tracker.update(client)

		assert tracker.war_enemy_player_ids == set()


class TestAllianceTrackerWars:
	def _active_war(
		self, *, attacker: str = "a1", defender: str = "enemy-alliance"
	) -> dict:
		return {
			"warId": "w1",
			"attackerAllianceId": attacker,
			"defenderAllianceId": defender,
			"status": "Active",
		}

	def test_war_enemy_player_ids_populated_when_attacker(self) -> None:
		tracker = AllianceTracker()
		war = self._active_war(attacker="a1", defender="enemy-alliance")
		client = _make_client(
			alliance_id="a1",
			wars=[war],
			enemy_member_ids=["e1", "e2"],
		)
		tracker.update(client)

		assert tracker.war_enemy_player_ids == {"e1", "e2"}

	def test_war_enemy_player_ids_populated_when_defender(self) -> None:
		tracker = AllianceTracker()
		war = self._active_war(attacker="enemy-alliance", defender="a1")
		client = _make_client(
			alliance_id="a1",
			wars=[war],
			enemy_member_ids=["e3"],
		)
		tracker.update(client)

		assert tracker.war_enemy_player_ids == {"e3"}

	def test_inactive_war_not_counted(self) -> None:
		tracker = AllianceTracker()
		war = {
			"warId": "w1",
			"attackerAllianceId": "a1",
			"defenderAllianceId": "enemy",
			"status": "PeaceProposed",
		}
		client = _make_client(wars=[war])
		tracker.update(client)

		assert tracker.war_enemy_player_ids == set()

	def test_is_war_target(self) -> None:
		tracker = AllianceTracker()
		war = self._active_war(attacker="a1", defender="enemy-alliance")
		client = _make_client(wars=[war], enemy_member_ids=["e1"])
		tracker.update(client)

		assert tracker.is_war_target("e1") is True
		assert tracker.is_war_target("neutral") is False

	def test_known_alliances_populated(self) -> None:
		tracker = AllianceTracker()
		all_alliances = [
			{"allianceId": "a1", "name": "Test", "memberCount": 2},
			{"allianceId": "a2", "name": "Other", "memberCount": 3},
		]
		client = _make_client(all_alliances=all_alliances)
		tracker.update(client)

		assert "a1" in tracker.known_alliances
		assert "a2" in tracker.known_alliances


class TestAllianceTrackerErrorTolerance:
	def test_api_failure_does_not_raise(self) -> None:
		tracker = AllianceTracker()
		client = MagicMock()
		client.get_my_alliance_status.side_effect = Exception("network error")

		# Should not raise.
		tracker.update(client)

	def test_detail_failure_does_not_crash_update(self) -> None:
		tracker = AllianceTracker()
		client = _make_client()
		client.get_alliance_detail.side_effect = Exception("timeout")

		tracker.update(client)
		# Still has the alliance ID from my-status.
		assert tracker.my_alliance_id == "a1"
