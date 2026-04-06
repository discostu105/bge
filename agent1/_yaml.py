"""Shared YAML loading helper."""
from __future__ import annotations

from pathlib import Path
from typing import Any


def load_yaml(path: Path) -> dict[str, Any]:
    try:
        import yaml  # type: ignore[import]
    except ImportError as exc:
        raise ImportError(
            "PyYAML is required for YAML config loading. "
            "Install it with: pip install pyyaml"
        ) from exc
    with open(path, encoding="utf-8") as fh:
        return yaml.safe_load(fh) or {}
