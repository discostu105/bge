"""Agent1 entry point — polling game loop wired to DecisionEngine."""
from __future__ import annotations

import argparse
import logging
import time
from collections import deque

from .client import BgeClient
from .config import AgentConfig, load_config
from .state.tracker import GameStateTracker
from .strategy.base import StrategyContext
from .strategy.dispatcher import ActionDispatcher
from .strategy.engine import DecisionEngine
from .strategy.registry import load_strategy


def run(config: AgentConfig) -> None:
	"""Run the bot game loop until interrupted.

	Each iteration polls ``/api/game/tick-info``.  When the server's
	``nextTickAt`` value changes (indicating a new game tick has been
	processed), state is fetched and ``DecisionEngine.run_tick`` is called
	exactly once.  Errors are caught and logged; the loop never crashes.
	"""
	logging.basicConfig(level=getattr(logging, config.log_level.upper(), logging.INFO))
	logger = logging.getLogger(__name__)

	client = BgeClient(config.base_url, config.api_key)
	tracker = GameStateTracker()
	dispatcher = ActionDispatcher(client)
	strategy = load_strategy(config.strategy)
	engine = DecisionEngine(strategy, dispatcher)
	history: deque = deque(maxlen=20)

	last_next_tick_at: str = ""

	logger.info(
		"Agent1 started — base_url=%s difficulty=%s strategy=%s poll=%ds",
		config.base_url,
		config.difficulty,
		config.strategy,
		config.poll_interval_seconds,
	)

	while True:
		try:
			tick_info = client.get_tick_info()
			next_tick_at: str = tick_info.get("nextTickAt", "")

			if next_tick_at != last_next_tick_at:
				last_next_tick_at = next_tick_at
				current_tick = hash(next_tick_at)

				resources = client.get_resources()
				units = client.get_units()
				attackable = client.get_attackable_players()

				tracker.update(tick_info, resources, units, attackable)

				ctx = StrategyContext(
					state=tracker,
					tick=current_tick,
					config=config,
					history=history,
				)
				engine.run_tick(ctx)
				history.append(tracker)

				logger.debug(
					"Tick processed — minerals=%d units=%d enemies=%d",
					tracker.minerals,
					tracker.units,
					len(tracker.attackable_players),
				)
		except Exception:
			logger.exception("Error in tick loop, continuing...")

		time.sleep(config.poll_interval_seconds)


def main() -> None:
	parser = argparse.ArgumentParser(description="Agent1 bot")
	parser.add_argument(
		"config_path",
		nargs="?",
		default=None,
		help="Path to YAML config file (default: config/config.yaml)",
	)
	args = parser.parse_args()
	run(load_config(args.config_path))


if __name__ == "__main__":
	main()
