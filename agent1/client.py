"""Thin HTTP client for the BGE REST API.

Uses only stdlib so no external dependencies are required.
Auth: ``Authorization: Bearer <api_key>`` where api_key starts with ``bge_k_``.
"""
from __future__ import annotations

import json
import urllib.error
import urllib.parse
import urllib.request
from typing import Any


class BgeClient:
	"""Synchronous HTTP client for BGE API endpoints used by the bot."""

	def __init__(self, base_url: str, api_key: str) -> None:
		self._base_url = base_url.rstrip("/")
		self._headers = {
			"Authorization": f"Bearer {api_key}",
			"Content-Type": "application/json",
			"Accept": "application/json",
		}

	def _get(self, path: str) -> Any:
		url = self._base_url + path
		req = urllib.request.Request(url, headers=self._headers)
		with urllib.request.urlopen(req) as resp:
			return json.loads(resp.read().decode())

	def _post(self, path: str, data: dict | None = None) -> Any:
		url = self._base_url + path
		body = json.dumps(data or {}).encode()
		req = urllib.request.Request(url, data=body, headers=self._headers, method="POST")
		try:
			with urllib.request.urlopen(req) as resp:
				raw = resp.read()
				return json.loads(raw.decode()) if raw.strip() else None
		except urllib.error.HTTPError as exc:
			if exc.code in (200, 204):
				return None
			raise

	def get_tick_info(self) -> dict:
		"""GET /api/game/tick-info → TickInfoViewModel."""
		return self._get("/api/game/tick-info")

	def get_resources(self) -> dict:
		"""GET /api/resources → PlayerResourcesViewModel."""
		return self._get("/api/resources")

	def get_units(self) -> dict:
		"""GET /api/units → UnitsViewModel."""
		return self._get("/api/units")

	def get_attackable_players(self) -> dict:
		"""GET /api/battle/attackableplayers → SelectEnemyViewModel."""
		return self._get("/api/battle/attackableplayers")

	def send_units(self, unit_id: str, enemy_player_id: str) -> None:
		"""POST /api/battle/sendunits — send a unit to attack an enemy."""
		eid = urllib.parse.quote(enemy_player_id, safe="")
		self._post(f"/api/battle/sendunits?unitId={unit_id}&enemyPlayerId={eid}")

	def build_units(self, unit_def_id: str, count: int) -> None:
		"""POST /api/units/build — train units of the given type."""
		self._post(f"/api/units/build?unitDefId={unit_def_id}&count={count}")

	def build_asset(self, asset_def_id: str) -> None:
		"""POST /api/assets/build — construct an asset."""
		self._post(f"/api/assets/build?assetDefId={asset_def_id}")

	def get_game_detail(self, game_id: str) -> dict:
		"""GET /api/games/{gameId} → GameDetailViewModel (status, winnerId, …)."""
		gid = urllib.parse.quote(game_id, safe="")
		return self._get(f"/api/games/{gid}")

	# --- Alliance API ---

	def get_my_alliance_status(self) -> dict:
		"""GET /api/alliances/my-status → MyAllianceStatusViewModel."""
		return self._get("/api/alliances/my-status")

	def get_all_alliances(self) -> list:
		"""GET /api/alliances → list[AllianceViewModel]."""
		return self._get("/api/alliances")

	def get_alliance_detail(self, alliance_id: str) -> dict:
		"""GET /api/alliances/{id} → AllianceDetailViewModel (includes member list)."""
		aid = urllib.parse.quote(alliance_id, safe="")
		return self._get(f"/api/alliances/{aid}")

	def get_alliance_wars(self, alliance_id: str) -> list:
		"""GET /api/alliances/{id}/wars → list[AllianceWarViewModel]."""
		aid = urllib.parse.quote(alliance_id, safe="")
		return self._get(f"/api/alliances/{aid}/wars")

	def create_alliance(self, name: str, password: str) -> str:
		"""POST /api/alliances — create a new alliance; returns the new alliance ID."""
		return self._post("/api/alliances", {"allianceName": name, "password": password})

	def join_alliance(self, alliance_id: str, password: str) -> None:
		"""POST /api/alliances/{id}/join — join an existing alliance."""
		aid = urllib.parse.quote(alliance_id, safe="")
		self._post(f"/api/alliances/{aid}/join", {"password": password})

	def declare_war(self, my_alliance_id: str, target_alliance_id: str) -> None:
		"""POST /api/alliances/{id}/declare-war — declare war on another alliance."""
		aid = urllib.parse.quote(my_alliance_id, safe="")
		self._post(f"/api/alliances/{aid}/declare-war", {"targetAllianceId": target_alliance_id})
