"""Agent1 entry point — loads config and runs a single strategy tick (stub)."""
from __future__ import annotations
import argparse
from .config import load_config
from .strategy.context import StrategyContext
from .strategy.military import should_attack
from .strategy.resources import worker_target, should_expand


def run_tick(ctx: StrategyContext) -> dict[str, object]:
    """Execute one decision tick and return a dict of recommended actions."""
    return {
        "attack": should_attack(ctx, enemy_units=0),
        "worker_target": worker_target(ctx, max_workers=10),
        "expand": should_expand(ctx),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Agent1 bot")
    parser.add_argument("config_path", nargs="?", default=None, help="Path to YAML config file")
    args = parser.parse_args()

    cfg = load_config(args.config_path)
    ctx = StrategyContext(
        difficulty_profile=cfg.difficulty_profile,
        overrides=cfg.strategy_overrides,
    )
    print(run_tick(ctx))


if __name__ == "__main__":
    main()
