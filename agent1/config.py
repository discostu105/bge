"""Agent1 configuration loader.

Configuration is resolved in this priority order (highest wins):
  1. Environment variables: BGE_DIFFICULTY, BGE_STRATEGY
  2. YAML config file (default path: config/config.yaml, or explicit path argument)
  3. Hard-coded defaults

Supported env vars:
  BGE_API_KEY     — API key for the BGE server
  BGE_GAME_ID     — Game ID to join
  BGE_BASE_URL    — BGE server base URL
  BGE_DIFFICULTY  — Difficulty level: easy | medium | hard | expert
  BGE_STRATEGY    — Strategy name: e.g. balanced
"""
from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from ._yaml import load_yaml as _load_yaml
from .strategy.difficulty import DifficultyProfile, PROFILES

_DEFAULT_CONFIG_PATH = Path("config/config.yaml")
_VALID_DIFFICULTIES = frozenset(PROFILES)


@dataclass
class AgentConfig:
    """Top-level configuration loaded once at agent startup."""

    poll_interval_seconds: int = 5
    log_level: str = "INFO"

    # Connection settings
    api_key: str = ""
    game_id: str = ""
    base_url: str = "https://ageofagents.net"

    # Strategy settings
    strategy: str = "balanced"

    # Human-readable difficulty name; validated against PROFILES on construction
    difficulty: str = "medium"

    # Fine-grained per-bot overrides; arbitrary key/value dict
    strategy_overrides: dict = field(default_factory=dict)

    # Resolved profile — kept in sync with difficulty
    difficulty_profile: DifficultyProfile = field(init=False)

    def __post_init__(self) -> None:
        key = self.difficulty.lower()
        if key not in _VALID_DIFFICULTIES:
            raise ValueError(
                f"Unknown difficulty {self.difficulty!r}. "
                f"Valid options: {sorted(_VALID_DIFFICULTIES)}"
            )
        self.difficulty_profile = PROFILES[key]


def load_config(config_path: str | Path | None = None) -> AgentConfig:
    """Load and return an AgentConfig.

    Args:
        config_path: Path to a YAML config file.  If *None*, the default path
            ``config/config.yaml`` is used when it exists (no error if absent).
            If an explicit path is given and the file does not exist,
            :exc:`FileNotFoundError` is raised.

    Returns:
        A fully resolved :class:`AgentConfig`.
    """
    raw: dict[str, Any] = {}

    if config_path is not None:
        path = Path(config_path)
        if not path.exists():
            raise FileNotFoundError(f"Config file not found: {path}")
        raw = _load_yaml(path)
    else:
        if _DEFAULT_CONFIG_PATH.exists():
            raw = _load_yaml(_DEFAULT_CONFIG_PATH)

    agent_raw: dict[str, Any] = raw.get("agent", {})

    # Env vars override YAML values
    difficulty = (
        os.environ.get("BGE_DIFFICULTY") or agent_raw.get("difficulty", "medium")
    ).lower()
    strategy = os.environ.get("BGE_STRATEGY") or agent_raw.get("strategy", "balanced")
    api_key = os.environ.get("BGE_API_KEY") or agent_raw.get("api_key", "")
    game_id = os.environ.get("BGE_GAME_ID") or agent_raw.get("game_id", "")
    base_url = (
        os.environ.get("BGE_BASE_URL")
        or agent_raw.get("base_url", "https://ageofagents.net")
    )

    strategy_overrides = dict(agent_raw.get("strategy_overrides") or {})

    return AgentConfig(
        poll_interval_seconds=int(agent_raw.get("poll_interval_seconds", 5)),
        log_level=str(agent_raw.get("log_level", "INFO")),
        api_key=api_key,
        game_id=game_id,
        base_url=base_url,
        strategy=strategy,
        difficulty=difficulty,
        strategy_overrides=strategy_overrides,
    )


