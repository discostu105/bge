from __future__ import annotations
from dataclasses import dataclass, field
from .difficulty import DifficultyProfile, MEDIUM


@dataclass
class StrategyContext:
    """Mutable game-state snapshot passed to every strategy module each tick."""

    # Current resources
    minerals: int = 0
    land: int = 0
    max_land: int = 0
    units: int = 0

    # Difficulty setting for this session
    difficulty_profile: DifficultyProfile = field(default_factory=lambda: MEDIUM)

    @property
    def land_ratio(self) -> float:
        """Fraction of maximum land that is occupied (0.0–1.0)."""
        if self.max_land == 0:
            return 0.0
        return self.land / self.max_land
