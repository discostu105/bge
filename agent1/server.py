"""Agent1 HTTP management server.

Exposes observability endpoints for the running bot.
Intended to be started in a daemon background thread from ``main.py``.

Current endpoints (Phase 4A — Strategy Analytics & Replay):
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
	from agent1.analytics.decision_logger import DecisionLogger
	from agent1.analytics.outcome_tracker import OutcomeTracker

logger = logging.getLogger(__name__)

_DEFAULT_LIMIT = 50
_MAX_LIMIT = 1000


def _parse_limit(query: str) -> int:
	"""Parse ``limit`` from a query string, clamping to ``[1, _MAX_LIMIT]``."""
	params = parse_qs(query)
	raw = params.get("limit", [str(_DEFAULT_LIMIT)])[0]
	try:
		value = int(raw)
	except ValueError:
		value = _DEFAULT_LIMIT
	return max(1, min(value, _MAX_LIMIT))


class _Handler(BaseHTTPRequestHandler):
	"""Request handler bound to shared analytics objects at class level."""

	decision_logger: DecisionLogger
	outcome_tracker: OutcomeTracker

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
		if parsed.path == "/replay":
			limit = _parse_limit(parsed.query)
			self._send_json(self.decision_logger.tail(limit))
		elif parsed.path == "/outcomes":
			limit = _parse_limit(parsed.query)
			self._send_json(self.outcome_tracker.tail(limit))
		else:
			self._send_json({"error": "not found"}, 404)


def start_server(
	decision_logger: DecisionLogger,
	outcome_tracker: OutcomeTracker,
	host: str = "0.0.0.0",
	port: int = 8081,
) -> HTTPServer:
	"""Start the management HTTP server in a daemon thread.

	Args:
		decision_logger: The :class:`~agent1.analytics.decision_logger.DecisionLogger`
			instance to expose via ``/replay``.
		outcome_tracker: The :class:`~agent1.analytics.outcome_tracker.OutcomeTracker`
			instance to expose via ``/outcomes``.
		host: Bind address.  Defaults to ``"0.0.0.0"``.
		port: Listen port.  Defaults to ``8081``.

	Returns:
		The running :class:`HTTPServer` instance (useful in tests).
	"""

	class _BoundHandler(_Handler):
		pass

	_BoundHandler.decision_logger = decision_logger
	_BoundHandler.outcome_tracker = outcome_tracker

	server = HTTPServer((host, port), _BoundHandler)
	thread = threading.Thread(target=server.serve_forever, name="agent1-server", daemon=True)
	thread.start()
	logger.info("Management server listening on http://%s:%d", host, port)
	return server
