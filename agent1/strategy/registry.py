"""Strategy registry — maps strategy names to strategy instances."""
from __future__ import annotations

from .balanced import BalancedStrategy

_REGISTRY: dict[str, type] = {
	"balanced": BalancedStrategy,
}


def load_strategy(name: str) -> object:
	"""Return a new strategy instance for *name*.

	Raises:
		ValueError: if *name* is not a registered strategy.
	"""
	if name not in _REGISTRY:
		raise ValueError(
			f"Unknown strategy {name!r}. Available strategies: {sorted(_REGISTRY)}"
		)
	return _REGISTRY[name]()
