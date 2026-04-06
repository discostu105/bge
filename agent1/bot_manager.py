"""Agent1 bot manager — spawns one subprocess per bot slot.

Each bot slot runs as an independent ``python -m agent1`` subprocess with its
credentials injected as environment variables.  This keeps crash isolation clean
and avoids shared mutable state between bots.

Usage::

    python -m agent1.bot_manager [config_path]

Config path defaults to ``config/bot_manager.yaml`` when not supplied.
"""
from __future__ import annotations

import os
import signal
import subprocess
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from ._yaml import load_yaml as _load_yaml

_DEFAULT_CONFIG_PATH = Path("config/bot_manager.yaml")


@dataclass
class BotSlot:
    """Configuration for a single bot instance."""

    name: str
    api_key: str
    game_id: str
    difficulty: str = "medium"
    strategy: str = "balanced"


@dataclass
class BotManagerConfig:
    """Top-level bot manager configuration."""

    base_url: str = "https://ageofagents.net"
    config_path: str | None = None
    bots: list[BotSlot] = field(default_factory=list)


def load_bot_manager_config(config_path: str | Path | None = None) -> BotManagerConfig:
    """Load BotManagerConfig from a YAML file.

    Args:
        config_path: Path to YAML.  Uses ``config/bot_manager.yaml`` when *None*
            and the default exists.  Raises :exc:`FileNotFoundError` when an
            explicit path is given and the file is absent.
    """
    raw: dict[str, Any] = {}

    if config_path is not None:
        path = Path(config_path)
        if not path.exists():
            raise FileNotFoundError(f"Bot manager config not found: {path}")
        raw = _load_yaml(path)
    else:
        if _DEFAULT_CONFIG_PATH.exists():
            raw = _load_yaml(_DEFAULT_CONFIG_PATH)

    bots = [
        BotSlot(
            name=b["name"],
            api_key=b["api_key"],
            game_id=b["game_id"],
            difficulty=b.get("difficulty", "medium"),
            strategy=b.get("strategy", "balanced"),
        )
        for b in raw.get("bots", [])
    ]

    return BotManagerConfig(
        base_url=raw.get("base_url", "https://ageofagents.net"),
        config_path=raw.get("config_path"),
        bots=bots,
    )


def _build_env(bot: BotSlot, base_url: str) -> dict[str, str]:
    """Build the subprocess environment for a single bot slot."""
    env = dict(os.environ)
    env["BGE_API_KEY"] = bot.api_key
    env["BGE_GAME_ID"] = bot.game_id
    env["BGE_DIFFICULTY"] = bot.difficulty
    env["BGE_STRATEGY"] = bot.strategy
    env["BGE_BASE_URL"] = base_url
    return env


def run(config_path: str | Path | None = None) -> None:
    """Load config, spawn one subprocess per bot slot, and supervise them."""
    cfg = load_bot_manager_config(config_path)

    if not cfg.bots:
        print("No bot slots configured — nothing to spawn.", file=sys.stderr)
        return

    processes: list[subprocess.Popen] = []

    def _shutdown(signum: int, frame: object) -> None:
        print(f"Received signal {signum} — terminating all bots.", file=sys.stderr)
        for proc in processes:
            proc.terminate()
        for proc in processes:
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                proc.kill()
        sys.exit(0)

    signal.signal(signal.SIGTERM, _shutdown)
    signal.signal(signal.SIGINT, _shutdown)

    for bot in cfg.bots:
        cmd = [sys.executable, "-m", "agent1"]
        if cfg.config_path:
            cmd.append(cfg.config_path)

        env = _build_env(bot, cfg.base_url)
        print(f"Spawning bot {bot.name!r} (difficulty={bot.difficulty})", file=sys.stderr)
        proc = subprocess.Popen(cmd, env=env)
        processes.append(proc)

    # Watch for unexpected exits
    while processes:
        for proc in list(processes):
            ret = proc.poll()
            if ret is not None:
                print(
                    f"Bot process (pid={proc.pid}) exited with code {ret}",
                    file=sys.stderr,
                )
                processes.remove(proc)
        time.sleep(1)


if __name__ == "__main__":
    _path = sys.argv[1] if len(sys.argv) > 1 else None
    run(_path)
