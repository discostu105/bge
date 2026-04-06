"""Agent1 HTTP management server.

Exposes control and observability endpoints for the running bot.
Intended to be started in a daemon background thread from ``main.py``.

Current endpoints (Phase 4B — Adaptive Difficulty):
  GET  /difficulty/recommendation  — current advisor output
  POST /difficulty/apply           — apply recommendation immediately

Phase 4A (Strategy Analytics & Replay) will add:
  GET  /replay?limit=N    — last N decision log records
  GET  /outcomes?limit=N  — last N outcome records
"""
from __future__ import annotations

import json
import logging
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer
from typing import TYPE_CHECKING
from urllib.parse import parse_qs, urlparse

if TYPE_CHECKING:
	from agent1.config import AgentConfig
	from agent1.strategy.adaptive_difficulty import AdaptiveDifficultyAdvisor

logger = logging.getLogger(__name__)


class _Handler(BaseHTTPRequestHandler):
	"""Request handler bound to a shared advisor and config at class level."""

	advisor: AdaptiveDifficultyAdvisor
	config: AgentConfig

	def log_message(self, fmt: str, *args: object) -> None:
		logger.debug(fmt, *args)

	def _send_json(self, data: object, status: int = 200) -> None:
		body = json.dumps(data).encode()
		self.send_response(status)
		self.send_header("Content-Type", "application/json")
		self.send_header("Content-Length", str(len(body)))
		self.end_headers()
		self.wfile.write(body)

	def do_GET(self) -> None:  # noqa: N802
		parsed = urlparse(self.path)
		if parsed.path == "/difficulty/recommendation":
			self._handle_recommendation()
		else:
			self._send_json({"error": "not found"}, 404)

	def do_POST(self) -> None:  # noqa: N802
		parsed = urlparse(self.path)
		if parsed.path == "/difficulty/apply":
			self._handle_apply()
		else:
			self._send_json({"error": "not found"}, 404)

	def _handle_recommendation(self) -> None:
		from agent1.strategy.adaptive_difficulty import INSUFFICIENT_DATA

		rec = self.advisor.recommend(self.config.difficulty)
		if rec == INSUFFICIENT_DATA:
			self._send_json({"status": INSUFFICIENT_DATA})
		else:
			self._send_json({
				"suggested_difficulty": rec.suggested_difficulty,
				"confidence": rec.confidence,
				"reason": rec.reason,
				"current_difficulty": self.config.difficulty,
			})

	def _handle_apply(self) -> None:
		from agent1.strategy.adaptive_difficulty import INSUFFICIENT_DATA
		from agent1.strategy.difficulty import PROFILES

		rec = self.advisor.recommend(self.config.difficulty)
		if rec == INSUFFICIENT_DATA:
			self._send_json({"status": INSUFFICIENT_DATA, "applied": False})
		else:
			old = self.config.difficulty
			self.config.difficulty = rec.suggested_difficulty
			self.config.difficulty_profile = PROFILES[rec.suggested_difficulty]
			logger.info(
				"Difficulty applied via HTTP: %s -> %s (reason: %s)",
				old,
				rec.suggested_difficulty,
				rec.reason,
			)
			self._send_json({
				"applied": True,
				"previous_difficulty": old,
				"new_difficulty": rec.suggested_difficulty,
				"reason": rec.reason,
			})


def start_server(
	advisor: AdaptiveDifficultyAdvisor,
	config: AgentConfig,
	host: str = "0.0.0.0",
	port: int = 8081,
) -> HTTPServer:
	"""Start the management HTTP server in a daemon thread.

	Args:
		advisor: The :class:`AdaptiveDifficultyAdvisor` instance to expose.
		config: The live :class:`AgentConfig` instance (mutated on apply).
		host: Bind address.  Defaults to ``"0.0.0.0"``.
		port: Listen port.  Defaults to ``8081``.

	Returns:
		The running :class:`HTTPServer` instance (useful in tests).
	"""

	class _BoundHandler(_Handler):
		pass

	_BoundHandler.advisor = advisor
	_BoundHandler.config = config

	server = HTTPServer((host, port), _BoundHandler)
	thread = threading.Thread(target=server.serve_forever, name="agent1-server", daemon=True)
	thread.start()
	logger.info("Management server listening on http://%s:%d", host, port)
	return server
