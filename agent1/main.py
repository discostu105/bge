"""Agent1 entry point — reads config and runs a single strategy tick (stub)."""
from __future__ import annotations
import argparse
from .config import AgentConfig
from .strategy.context import StrategyContext
from .strategy.military import should_attack
from .strategy.resources import worker_target, should_expand


def run_tick(cfg: AgentConfig, ctx: StrategyContext) -> dict[str, object]:
    """Execute one decision tick and return a dict of recommended actions."""
    return {
        "attack": should_attack(ctx, enemy_units=0),
        "worker_target": worker_target(ctx, max_workers=10),
        "expand": should_expand(ctx),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Agent1 bot")
    parser.add_argument("--difficulty", default="medium", help="Difficulty level")
    args = parser.parse_args()

    cfg = AgentConfig(difficulty=args.difficulty)
    ctx = StrategyContext(difficulty_profile=cfg.difficulty_profile)
    print(run_tick(cfg, ctx))


if __name__ == "__main__":
    main()
