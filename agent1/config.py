from __future__ import annotations
from dataclasses import dataclass, field
from .strategy.difficulty import DifficultyProfile, MEDIUM, PROFILES


@dataclass
class AgentConfig:
    """Top-level configuration loaded once at agent startup."""

    # Human-readable difficulty name; resolved to a DifficultyProfile on construction
    difficulty: str = "medium"

    # Resolved profile — kept in sync with ``difficulty``
    difficulty_profile: DifficultyProfile = field(init=False)

    def __post_init__(self) -> None:
        key = self.difficulty.lower()
        if key not in PROFILES:
            raise ValueError(
                f"Unknown difficulty {self.difficulty!r}. "
                f"Valid options: {sorted(PROFILES)}"
            )
        self.difficulty_profile = PROFILES[key]
