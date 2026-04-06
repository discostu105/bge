from __future__ import annotations
from .context import StrategyContext


def should_attack(ctx: StrategyContext, enemy_units: int) -> bool:
    """Return True when the bot should launch an attack this tick.

    The decision is scaled by ``difficulty_profile.aggression``:
    - EASY  (0.1) — only attacks when outnumbering 5-to-1
    - EXPERT (1.0) — attacks even when slightly outnumbered
    """
    if enemy_units <= 0:
        return True
    ratio = ctx.units / enemy_units
    # required ratio decreases as aggression increases (min 0.8 at expert)
    required_ratio = 5.0 - 4.2 * ctx.difficulty_profile.aggression
    return ratio >= required_ratio
