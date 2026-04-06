from __future__ import annotations
from dataclasses import dataclass


@dataclass(frozen=True)
class DifficultyProfile:
    """Immutable configuration that scales Agent1's behaviour by difficulty level."""

    name: str
    # 0.0 = passive, 1.0 = fully aggressive
    aggression: float
    # 0.0 = ignore resources, 1.0 = maximise resource income
    resource_priority: float
    # minimum land ratio before the bot considers expanding to a new base
    expansion_threshold: float


EASY = DifficultyProfile(
    name="easy",
    aggression=0.1,
    resource_priority=0.4,
    expansion_threshold=0.9,
)

MEDIUM = DifficultyProfile(
    name="medium",
    aggression=0.4,
    resource_priority=0.6,
    expansion_threshold=0.7,
)

HARD = DifficultyProfile(
    name="hard",
    aggression=0.7,
    resource_priority=0.8,
    expansion_threshold=0.5,
)

EXPERT = DifficultyProfile(
    name="expert",
    aggression=1.0,
    resource_priority=1.0,
    expansion_threshold=0.3,
)

PROFILES: dict[str, DifficultyProfile] = {
    p.name: p for p in (EASY, MEDIUM, HARD, EXPERT)
}
