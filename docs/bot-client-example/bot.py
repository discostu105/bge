#!/usr/bin/env python3
"""
BGE Bot Client Example
======================
A minimal bot that plays Browser Game Engine automatically.

Usage:
    export BGE_BASE_URL=https://ageofagents.net
    export BGE_API_KEY=bge_k_<your-key>
    python bot.py

Get your API key from the Player Management page in the BGE web UI,
or by calling POST /api/players/{playerId}/apikey with a session token.
"""

import os
import sys
import time
import logging
from datetime import datetime, timezone
from typing import Optional

import requests  # pip install requests

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

BASE_URL = os.environ.get("BGE_BASE_URL", "http://localhost:5000").rstrip("/")
API_KEY  = os.environ.get("BGE_API_KEY", "")

# How many seconds before the next tick to wake up and issue commands.
# A small positive value (1-2 s) gives the tick time to be processed server-side.
WAKE_BEFORE_TICK_SECS = 2

# Worker split: fraction of workers dedicated to minerals vs gas.
# Adjust to suit the game's economy.
MINERAL_WORKER_FRACTION = 0.7

logging.basicConfig(level=logging.INFO, format="%(asctime)s  %(levelname)s  %(message)s")
log = logging.getLogger("bge-bot")

# ---------------------------------------------------------------------------
# HTTP helpers
# ---------------------------------------------------------------------------

SESSION = requests.Session()
SESSION.headers.update({"Authorization": f"Bearer {API_KEY}"})


def get(path: str, params: dict = None) -> dict:
    """Issue a GET request and return the parsed JSON body."""
    resp = SESSION.get(f"{BASE_URL}{path}", params=params)
    resp.raise_for_status()
    return resp.json()


def post(path: str, params: dict = None, json: dict = None) -> Optional[dict]:
    """Issue a POST request. Returns parsed JSON if the response has a body."""
    resp = SESSION.post(f"{BASE_URL}{path}", params=params, json=json)
    if resp.status_code == 400:
        # Bad request — log and continue rather than crashing the bot.
        log.warning("POST %s returned 400: %s", path, resp.text)
        return None
    resp.raise_for_status()
    return resp.json() if resp.text else None


# ---------------------------------------------------------------------------
# Game-loop helpers
# ---------------------------------------------------------------------------

def get_tick_info() -> dict:
    """Return the tick-info payload (serverTime, nextTickAt, unreadMessageCount)."""
    return get("/api/game/tick-info")


def sleep_until(iso_timestamp: str) -> None:
    """Sleep until the given ISO 8601 UTC timestamp, minus WAKE_BEFORE_TICK_SECS."""
    next_tick = datetime.fromisoformat(iso_timestamp.replace("Z", "+00:00"))
    now       = datetime.now(timezone.utc)
    delay     = (next_tick - now).total_seconds() - WAKE_BEFORE_TICK_SECS
    if delay > 0:
        log.info("Sleeping %.1f s until next tick at %s", delay, iso_timestamp)
        time.sleep(delay)


# ---------------------------------------------------------------------------
# Strategy: workers
# ---------------------------------------------------------------------------

def manage_workers() -> None:
    """Keep all workers assigned; prefer minerals over gas."""
    info = get("/api/workers")
    total    = info["totalWorkers"]
    mineral  = int(total * MINERAL_WORKER_FRACTION)
    gas      = total - mineral

    current_mineral = info["mineralWorkers"]
    current_gas     = info["gasWorkers"]

    if current_mineral != mineral or current_gas != gas:
        log.info("Reassigning workers: %d minerals, %d gas (was %d/%d)",
                 mineral, gas, current_mineral, current_gas)
        post("/api/workers/Assign", params={"mineralWorkers": mineral, "gasWorkers": gas})
    else:
        log.info("Workers already optimal (%d/%d)", mineral, gas)


# ---------------------------------------------------------------------------
# Strategy: buildings
# ---------------------------------------------------------------------------

# Build order: try each asset in sequence; skip if already built or unaffordable.
# Replace these IDs with valid assetDefIds from your game definition.
BUILD_ORDER = [
    "commandcenter",
    "barracks",
    "factory",
    "starport",
]


def manage_assets() -> None:
    """Queue the next building in the build order if not already built."""
    assets = get("/api/assets")
    built_ids = {a["definition"]["id"] for a in assets.get("assets", []) if a.get("built")}

    for asset_def_id in BUILD_ORDER:
        if asset_def_id not in built_ids:
            log.info("Attempting to build: %s", asset_def_id)
            post("/api/assets/Build", params={"assetDefId": asset_def_id})
            # Only queue one building at a time.
            return

    log.info("Build order complete; nothing to build")


# ---------------------------------------------------------------------------
# Strategy: units
# ---------------------------------------------------------------------------

UNIT_TO_TRAIN  = "marine"   # unitDefId — replace with a valid unit from your game.
UNITS_PER_TICK = 3          # How many to queue each tick (if affordable).


def manage_units() -> None:
    """Train a fixed number of combat units per tick, then merge all stacks."""
    log.info("Training %d x %s", UNITS_PER_TICK, UNIT_TO_TRAIN)
    post("/api/units/Build", params={"unitDefId": UNIT_TO_TRAIN, "count": UNITS_PER_TICK})

    # Merging keeps stacks tidy and makes SendUnits simpler.
    post("/api/units/Merge")


# ---------------------------------------------------------------------------
# Strategy: attack
# ---------------------------------------------------------------------------

def attack_weakest() -> None:
    """Attack the lowest-ranked player we can reach."""
    # Fetch the global ranking to find the weakest opponent.
    all_players = get("/api/playerranking")

    # Fetch only players we're actually allowed to attack.
    attackable  = get("/api/battle/AttackablePlayers")
    attackable_ids = {p["playerId"] for p in attackable.get("attackablePlayers", [])}

    if not attackable_ids:
        log.info("No attackable players this tick")
        return

    # Pick the weakest (highest rank number = worst rank) attackable player.
    candidates = [p for p in all_players if p["playerId"] in attackable_ids]
    if not candidates:
        log.info("No matching attackable players in ranking list")
        return

    target = min(candidates, key=lambda p: p.get("score", 0))
    target_id   = target["playerId"]
    target_name = target.get("playerName", target_id)

    # Scout first so we know what we're walking into.
    enemy_base = get("/api/battle/EnemyBase", params={"enemyPlayerId": target_id})
    defender_count = sum(
        u.get("count", 0) for u in enemy_base.get("enemyDefendingUnits", {}).get("units", [])
    )
    log.info("Target: %s (rank %s, %d defenders)", target_name, target.get("rank"), defender_count)

    # Get our unit stacks.
    our_units = get("/api/units").get("units", [])
    if not our_units:
        log.info("No units to attack with")
        return

    # Use our largest unit stack.
    biggest_stack = max(our_units, key=lambda u: u.get("count", 0))
    attacker_count = biggest_stack.get("count", 0)

    if attacker_count <= defender_count:
        log.info("Skipping attack: %d attackers vs %d defenders — odds too poor",
                 attacker_count, defender_count)
        return

    # Move units to the target, then attack.
    unit_id = biggest_stack["unitId"]
    log.info("Sending %d units (stack %s) at %s", attacker_count, unit_id, target_name)
    post("/api/battle/SendUnits", params={"unitId": unit_id, "enemyPlayerId": target_id})

    result = post("/api/battle/Attack", params={"enemyPlayerId": target_id})
    if result:
        log.info("Battle result: %s", result)


# ---------------------------------------------------------------------------
# Main loop
# ---------------------------------------------------------------------------

def run_tick() -> None:
    """Execute one full tick of bot logic."""
    log.info("=== Tick starting ===")
    manage_workers()
    manage_assets()
    manage_units()
    attack_weakest()
    log.info("=== Tick done ===")


def main() -> None:
    if not API_KEY:
        sys.exit("BGE_API_KEY environment variable is not set. "
                 "Generate a key on the Player Management page.")

    log.info("BGE bot starting. Base URL: %s", BASE_URL)

    while True:
        try:
            tick_info = get_tick_info()
            run_tick()
            # Sleep until just before the next tick.
            sleep_until(tick_info["nextTickAt"])
        except requests.HTTPError as exc:
            log.error("HTTP error: %s", exc)
            time.sleep(10)  # Back off briefly before retrying.
        except KeyboardInterrupt:
            log.info("Bot stopped by user")
            break


if __name__ == "__main__":
    main()
