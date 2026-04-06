"""Agent1 entry point — polling game loop wired to DecisionEngine."""
from __future__ import annotations

import argparse
import logging
import time
from collections import deque

from .client import BgeClient
from .config import AgentConfig, load_config
from .server import start_server
from .state.alliance_tracker import AllianceTracker
from .state.tracker import GameStateTracker
from .strategy.adaptive_difficulty import INSUFFICIENT_DATA, AdaptiveDifficultyAdvisor
from .strategy.base import StrategyContext
from .strategy.difficulty import PROFILES
from .strategy.dispatcher import ActionDispatcher
from .strategy.engine import DecisionEngine
from .strategy.registry import load_alliance_module, load_strategy

# Confidence threshold to trigger an automatic difficulty adjustment.
_AUTO_ADJUST_CONFIDENCE = 0.5


def _apply_difficulty_recommendation(
	advisor: AdaptiveDifficultyAdvisor,
	config: AgentConfig,
	logger: logging.Logger,
) -> None:
	"""Ask the advisor for a recommendation and apply it when confident enough."""
	rec = advisor.recommend(config.difficulty)
	if rec == INSUFFICIENT_DATA:
		logger.info("Adaptive difficulty: insufficient data, keeping %s", config.difficulty)
		return
	if rec.suggested_difficulty != config.difficulty and rec.confidence >= _AUTO_ADJUST_CONFIDENCE:
		old = config.difficulty
		config.difficulty = rec.suggested_difficulty
		config.difficulty_profile = PROFILES[rec.suggested_difficulty]
		logger.info(
			"Adaptive difficulty: %s -> %s (confidence=%.2f) — %s",
			old,
			rec.suggested_difficulty,
			rec.confidence,
			rec.reason,
		)
	else:
		logger.info("Adaptive difficulty: keeping %s — %s", config.difficulty, rec.reason)


def run(config: AgentConfig) -> None:
	"""Run the bot game loop until interrupted.

	Each iteration polls ``/api/game/tick-info``.  When the server's
	``nextTickAt`` value changes (indicating a new game tick has been
	processed), state is fetched and ``DecisionEngine.run_tick`` is called
	exactly once.  Errors are caught and logged; the loop never crashes.

	Phase 4B additions:
	- Starts the HTTP management server (port 8081 by default).
	- Checks game status each tick; when the game transitions to ``"Finished"``,
	  calls ``AdaptiveDifficultyAdvisor`` and optionally updates difficulty.
	"""
	logging.basicConfig(level=getattr(logging, config.log_level.upper(), logging.INFO))
	logger = logging.getLogger(__name__)

	client = BgeClient(config.base_url, config.api_key)
	tracker = GameStateTracker()
	alliance_tracker = AllianceTracker()
	dispatcher = ActionDispatcher(client)
	strategy = load_strategy(config.strategy)
	alliance_module = load_alliance_module()
	engine = DecisionEngine(strategy, dispatcher, alliance_module=alliance_module)
	history: deque = deque(maxlen=20)
	advisor = AdaptiveDifficultyAdvisor()

	start_server(advisor, config)

	last_next_tick_at: str = ""
	last_game_status: str = ""

	logger.info(
		"Agent1 started — base_url=%s difficulty=%s strategy=%s alliance_mode=%s poll=%ds",
		config.base_url,
		config.difficulty,
		config.strategy,
		config.alliance_mode,
		config.poll_interval_seconds,
	)

	while True:
		try:
			tick_info = client.get_tick_info()
			next_tick_at: str = tick_info.get("nextTickAt", "")

			if next_tick_at != last_next_tick_at:
				last_next_tick_at = next_tick_at
				current_tick = hash(next_tick_at)

				# Refresh alliance state before strategy decisions.
				alliance_tracker.update(client)

				resources = client.get_resources()
				units = client.get_units()
				attackable = client.get_attackable_players()

				tracker.update(tick_info, resources, units, attackable)

				ctx = StrategyContext(
					state=tracker,
					tick=current_tick,
					config=config,
					history=history,
					alliance_tracker=alliance_tracker,
				)
				engine.run_tick(ctx)
				history.append(tracker)

				logger.debug(
					"Tick processed — minerals=%d units=%d enemies=%d ally_count=%d war_enemies=%d",
					tracker.minerals,
					tracker.units,
					len(tracker.attackable_players),
					len(alliance_tracker.allied_player_ids),
					len(alliance_tracker.war_enemy_player_ids),
				)

				# Detect game end and trigger adaptive difficulty adjustment.
				if config.game_id:
					try:
						game_detail = client.get_game_detail(config.game_id)
						status: str = game_detail.get("status", "")
						if status == "Finished" and last_game_status != "Finished":
							logger.info("Game %s finished — evaluating difficulty adjustment", config.game_id)
							_apply_difficulty_recommendation(advisor, config, logger)
						last_game_status = status
					except Exception:
						logger.debug("Could not fetch game status for %s", config.game_id, exc_info=True)
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
