from __future__ import annotations
from .context import StrategyContext


def worker_target(ctx: StrategyContext, max_workers: int) -> int:
    """Return the desired number of mineral workers.

    Scales with ``difficulty_profile.resource_priority``:
    - EASY  (0.4) — allocates 40 % of max workers to mining
    - EXPERT (1.0) — fills all available worker slots
    """
    return max(1, round(max_workers * ctx.difficulty_profile.resource_priority))


def should_expand(ctx: StrategyContext) -> bool:
    """Return True when the bot should colonise a new land tile.

    Expansion is triggered when land fill exceeds the profile threshold:
    - EASY  (0.9) — expands only when almost full
    - EXPERT (0.3) — expands aggressively at 30 % fill
    """
    return ctx.land_ratio >= ctx.difficulty_profile.expansion_threshold
