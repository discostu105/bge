"""Tests for agent1.config — load_config() and AgentConfig env-var wiring."""
from __future__ import annotations

import os
import textwrap
from pathlib import Path

import pytest

from agent1.config import AgentConfig, load_config


# ---------------------------------------------------------------------------
# AgentConfig dataclass
# ---------------------------------------------------------------------------

class TestAgentConfig:
    def test_strategy_overrides_defaults_to_empty_dict(self):
        cfg = AgentConfig()
        assert cfg.strategy_overrides == {}

    def test_strategy_defaults_to_balanced(self):
        cfg = AgentConfig()
        assert cfg.strategy == "balanced"

    def test_all_valid_difficulties_accepted(self, all_difficulties):
        name, _ = all_difficulties
        cfg = AgentConfig(difficulty=name)
        assert cfg.difficulty == name

    @pytest.fixture(params=["easy", "medium", "hard", "expert"])
    def all_difficulties(self, request):
        return request.param, request.param


# ---------------------------------------------------------------------------
# load_config() — env-var overrides
# ---------------------------------------------------------------------------

class TestLoadConfigEnvVars:
    def test_bge_difficulty_env_var_overrides_yaml(self, tmp_path, monkeypatch):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text(textwrap.dedent("""\
            agent:
              difficulty: easy
        """))
        monkeypatch.setenv("BGE_DIFFICULTY", "hard")
        cfg = load_config(yaml_file)
        assert cfg.difficulty == "hard"

    def test_bge_strategy_env_var_overrides_yaml(self, tmp_path, monkeypatch):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text(textwrap.dedent("""\
            agent:
              strategy: balanced
        """))
        monkeypatch.setenv("BGE_STRATEGY", "aggressive")
        cfg = load_config(yaml_file)
        assert cfg.strategy == "aggressive"

    def test_bge_api_key_env_var_overrides_yaml(self, tmp_path, monkeypatch):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text("agent:\n  api_key: from_yaml\n")
        monkeypatch.setenv("BGE_API_KEY", "from_env")
        cfg = load_config(yaml_file)
        assert cfg.api_key == "from_env"

    def test_bge_game_id_env_var_overrides_yaml(self, tmp_path, monkeypatch):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text("agent:\n  game_id: yaml_game\n")
        monkeypatch.setenv("BGE_GAME_ID", "env_game")
        cfg = load_config(yaml_file)
        assert cfg.game_id == "env_game"


# ---------------------------------------------------------------------------
# load_config() — validation
# ---------------------------------------------------------------------------

class TestLoadConfigValidation:
    def test_invalid_difficulty_raises_value_error(self, tmp_path, monkeypatch):
        monkeypatch.setenv("BGE_DIFFICULTY", "godmode")
        with pytest.raises(ValueError, match="Unknown difficulty"):
            load_config()

    def test_explicit_missing_path_raises_file_not_found(self):
        with pytest.raises(FileNotFoundError):
            load_config("/nonexistent/path/config.yaml")

    def test_all_valid_difficulty_values_accepted(self, tmp_path):
        for name in ("easy", "medium", "hard", "expert"):
            yaml_file = tmp_path / f"config_{name}.yaml"
            yaml_file.write_text(f"agent:\n  difficulty: {name}\n")
            cfg = load_config(yaml_file)
            assert cfg.difficulty == name


# ---------------------------------------------------------------------------
# load_config() — YAML values
# ---------------------------------------------------------------------------

class TestLoadConfigYaml:
    def test_strategy_overrides_loaded_from_yaml(self, tmp_path):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text(textwrap.dedent("""\
            agent:
              strategy_overrides:
                attack_unit_threshold: 15
                worker_target_cap: 8
        """))
        cfg = load_config(yaml_file)
        assert cfg.strategy_overrides == {"attack_unit_threshold": 15, "worker_target_cap": 8}

    def test_strategy_overrides_defaults_to_empty_when_absent(self, tmp_path):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text("agent:\n  difficulty: medium\n")
        cfg = load_config(yaml_file)
        assert cfg.strategy_overrides == {}

    def test_poll_interval_loaded_from_yaml(self, tmp_path):
        yaml_file = tmp_path / "config.yaml"
        yaml_file.write_text("agent:\n  poll_interval_seconds: 10\n")
        cfg = load_config(yaml_file)
        assert cfg.poll_interval_seconds == 10

    def test_no_config_file_uses_defaults(self, tmp_path, monkeypatch):
        # Ensure default path does not exist
        monkeypatch.chdir(tmp_path)
        cfg = load_config()
        assert cfg.difficulty == "medium"
        assert cfg.strategy == "balanced"
        assert cfg.strategy_overrides == {}
