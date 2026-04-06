"""Agent1 tournament coordinator — creates a series of tagged games and polls for results.

Usage::

    python -m agent1.tournament [config_path]

Config path defaults to ``config/tournament.yaml``.

Required env vars:
    BGE_ADMIN_API_KEY  — admin API key (needed for bot slot creation)
    BGE_BASE_URL       — BGE server base URL (default: https://ageofagents.net)

Config file (YAML)::

    tournament_id: ""          # auto-generated if empty
    rounds: 3
    game_duration_minutes: 60
    bots_per_game: 2
    difficulty: "medium"
    game_def_type: "sco"
    tick_duration: "00:00:10"
"""
from __future__ import annotations

import os
import sys
import time
import uuid
import datetime
import argparse
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import subprocess
import urllib.request
import urllib.error
import json

from ._yaml import load_yaml as _load_yaml
from .bot_manager import BotSlot, _build_env, load_bot_manager_config

_DEFAULT_CONFIG_PATH = Path("config/tournament.yaml")


@dataclass
class TournamentConfig:
    tournament_id: str = ""
    rounds: int = 3
    game_duration_minutes: int = 60
    bots_per_game: int = 2
    difficulty: str = "medium"
    game_def_type: str = "sco"
    tick_duration: str = "00:00:10"


def load_tournament_config(config_path: str | Path | None = None) -> TournamentConfig:
    raw: dict[str, Any] = {}
    path = Path(config_path) if config_path else _DEFAULT_CONFIG_PATH
    if path.exists():
        raw = _load_yaml(path) or {}
    cfg = TournamentConfig()
    cfg.tournament_id = raw.get("tournament_id", cfg.tournament_id)
    cfg.rounds = int(raw.get("rounds", cfg.rounds))
    cfg.game_duration_minutes = int(raw.get("game_duration_minutes", cfg.game_duration_minutes))
    cfg.bots_per_game = int(raw.get("bots_per_game", cfg.bots_per_game))
    cfg.difficulty = raw.get("difficulty", cfg.difficulty)
    cfg.game_def_type = raw.get("game_def_type", cfg.game_def_type)
    cfg.tick_duration = raw.get("tick_duration", cfg.tick_duration)
    return cfg


def _api_request(base_url: str, path: str, method: str = "GET", payload: dict | None = None, api_key: str = "") -> Any:
    url = base_url.rstrip("/") + path
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if api_key:
        req.add_header("X-Api-Key", api_key)
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def _create_game(base_url: str, api_key: str, name: str, tournament_id: str,
                 start_time: datetime.datetime, end_time: datetime.datetime,
                 game_def_type: str, tick_duration: str) -> str:
    payload = {
        "name": name,
        "gameDefType": game_def_type,
        "startTime": start_time.isoformat(),
        "endTime": end_time.isoformat(),
        "tickDuration": tick_duration,
        "tournamentId": tournament_id,
    }
    result = _api_request(base_url, "/api/games", method="POST", payload=payload, api_key=api_key)
    return result["gameId"]


def _get_game_status(base_url: str, game_id: str, api_key: str = "") -> str:
    result = _api_request(base_url, f"/api/games/{game_id}", api_key=api_key)
    return result.get("status", "Unknown")


def _get_tournament_results(base_url: str, tournament_id: str) -> dict:
    return _api_request(base_url, f"/api/tournaments/{tournament_id}/results")


def run_tournament(config_path: str | None = None) -> None:
    cfg = load_tournament_config(config_path)

    base_url = os.environ.get("BGE_BASE_URL", "https://ageofagents.net")
    admin_api_key = os.environ.get("BGE_ADMIN_API_KEY", "")
    if not admin_api_key:
        print("ERROR: BGE_ADMIN_API_KEY environment variable is required.", file=sys.stderr)
        sys.exit(1)

    tournament_id = cfg.tournament_id.strip() or f"tournament-{uuid.uuid4().hex[:8]}"
    print(f"Tournament ID: {tournament_id}")
    print(f"Rounds: {cfg.rounds}, Duration: {cfg.game_duration_minutes}m/game, Bots/game: {cfg.bots_per_game}")

    game_ids: list[str] = []
    bot_processes: list[subprocess.Popen] = []
    now = datetime.datetime.utcnow()

    for round_num in range(1, cfg.rounds + 1):
        offset_minutes = (round_num - 1) * cfg.game_duration_minutes
        start_time = now + datetime.timedelta(minutes=offset_minutes)
        end_time = start_time + datetime.timedelta(minutes=cfg.game_duration_minutes)
        game_name = f"{tournament_id} Round {round_num}"

        print(f"Creating game: {game_name} ...")
        try:
            game_id = _create_game(
                base_url=base_url,
                api_key=admin_api_key,
                name=game_name,
                tournament_id=tournament_id,
                start_time=start_time,
                end_time=end_time,
                game_def_type=cfg.game_def_type,
                tick_duration=cfg.tick_duration,
            )
            game_ids.append(game_id)
            print(f"  Created game {game_id}")
        except Exception as exc:
            print(f"  ERROR creating game for round {round_num}: {exc}", file=sys.stderr)
            continue

        for bot_num in range(1, cfg.bots_per_game + 1):
            bot_name = f"Bot-{bot_num}"
            slot = BotSlot(
                name=bot_name,
                api_key=admin_api_key,
                game_id=game_id,
                difficulty=cfg.difficulty,
            )
            env = _build_env(slot, base_url)
            proc = subprocess.Popen([sys.executable, "-m", "agent1"], env=env)
            bot_processes.append(proc)
            print(f"  Spawned bot {bot_name!r} (pid={proc.pid}, difficulty={cfg.difficulty})")

    if not game_ids:
        print("No games were created. Exiting.", file=sys.stderr)
        sys.exit(1)

    print(f"\nWaiting for {len(game_ids)} game(s) to finish ...")
    poll_interval = 30
    while True:
        statuses = {}
        for game_id in game_ids:
            try:
                statuses[game_id] = _get_game_status(base_url, game_id, api_key=admin_api_key)
            except Exception as exc:
                statuses[game_id] = f"ERROR({exc})"

        finished = sum(1 for s in statuses.values() if s == "Finished")
        print(f"  {finished}/{len(game_ids)} games finished")

        if finished == len(game_ids):
            break
        time.sleep(poll_interval)

    for proc in bot_processes:
        proc.terminate()

    print(f"\nAll games finished. Fetching tournament results for {tournament_id} ...")
    try:
        results = _get_tournament_results(base_url, tournament_id)
        print(f"\n=== Tournament Results: {tournament_id} ===")
        print(f"Total games: {results['totalGames']}")
        print(f"{'Rank':<6} {'Player':<30} {'Games':<7} {'Wins':<6} {'Score'}")
        print("-" * 60)
        for r in results.get("rankings", []):
            print(f"{r['rank']:<6} {r['playerName']:<30} {r['gamesPlayed']:<7} {r['wins']:<6} {r['totalScore']:.1f}")
    except Exception as exc:
        print(f"ERROR fetching results: {exc}", file=sys.stderr)


def main() -> None:
    parser = argparse.ArgumentParser(description="BGE tournament coordinator")
    parser.add_argument("config_path", nargs="?", default=None, help="Path to tournament YAML config")
    args = parser.parse_args()
    run_tournament(args.config_path)


if __name__ == "__main__":
    main()
