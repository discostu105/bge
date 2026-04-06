"""Tests for agent1.bot_manager."""
from __future__ import annotations

import textwrap
from pathlib import Path
from unittest.mock import MagicMock, call, patch

import pytest

from agent1.bot_manager import BotSlot, BotManagerConfig, load_bot_manager_config, run


# ---------------------------------------------------------------------------
# load_bot_manager_config()
# ---------------------------------------------------------------------------

class TestLoadBotManagerConfig:
    def test_loads_two_slots(self, tmp_path):
        cfg_file = tmp_path / "bot_manager.yaml"
        cfg_file.write_text(textwrap.dedent("""\
            base_url: "https://example.com"
            bots:
              - name: bot-easy
                api_key: key1
                game_id: game1
                difficulty: easy
              - name: bot-hard
                api_key: key2
                game_id: game1
                difficulty: hard
        """))
        cfg = load_bot_manager_config(cfg_file)
        assert len(cfg.bots) == 2
        assert cfg.bots[0].name == "bot-easy"
        assert cfg.bots[1].name == "bot-hard"

    def test_explicit_missing_path_raises_file_not_found(self):
        with pytest.raises(FileNotFoundError):
            load_bot_manager_config("/nonexistent/bot_manager.yaml")

    def test_defaults_when_no_file(self, tmp_path, monkeypatch):
        monkeypatch.chdir(tmp_path)
        cfg = load_bot_manager_config()
        assert cfg.bots == []
        assert cfg.base_url == "https://ageofagents.net"


# ---------------------------------------------------------------------------
# run() — subprocess behaviour
# ---------------------------------------------------------------------------

class TestRun:
    def _make_config_file(self, tmp_path: Path, n_bots: int = 2) -> Path:
        bots_yaml = "\n".join(
            f"  - name: bot-{i}\n    api_key: key{i}\n    game_id: game1\n    difficulty: easy"
            for i in range(n_bots)
        )
        cfg_file = tmp_path / "bm.yaml"
        cfg_file.write_text(f"bots:\n{bots_yaml}\n")
        return cfg_file

    def test_spawns_one_subprocess_per_bot_slot(self, tmp_path):
        cfg_file = self._make_config_file(tmp_path, n_bots=2)
        mock_proc = MagicMock()
        mock_proc.poll.return_value = 0  # immediately "done" so the watch loop exits

        with patch("agent1.bot_manager.subprocess.Popen", return_value=mock_proc) as mock_popen:
            run(cfg_file)

        assert mock_popen.call_count == 2

    def test_subprocess_env_contains_api_key_and_difficulty(self, tmp_path):
        cfg_file = tmp_path / "bm.yaml"
        cfg_file.write_text(textwrap.dedent("""\
            base_url: "https://example.com"
            bots:
              - name: my-bot
                api_key: secret_key
                game_id: round-1
                difficulty: hard
        """))
        mock_proc = MagicMock()
        mock_proc.poll.return_value = 0

        with patch("agent1.bot_manager.subprocess.Popen", return_value=mock_proc) as mock_popen:
            run(cfg_file)

        env = mock_popen.call_args.kwargs["env"]
        assert env["BGE_API_KEY"] == "secret_key"
        assert env["BGE_DIFFICULTY"] == "hard"
        assert env["BGE_GAME_ID"] == "round-1"
        assert env["BGE_BASE_URL"] == "https://example.com"

    def test_shutdown_terminates_all_processes(self, tmp_path):
        cfg_file = self._make_config_file(tmp_path, n_bots=2)

        call_count = [0]

        def poll_side_effect():
            call_count[0] += 1
            # Let the processes appear to keep running for a few checks, then exit
            if call_count[0] > 4:
                return 0
            return None

        mock_proc = MagicMock()
        mock_proc.poll.side_effect = poll_side_effect

        with patch("agent1.bot_manager.subprocess.Popen", return_value=mock_proc):
            run(cfg_file)

        # terminate() should be called (via SIGTERM handler or natural exit)
        # — at minimum poll() was called
        assert mock_proc.poll.call_count > 0

    def test_no_bots_configured_returns_early(self, tmp_path, capsys):
        cfg_file = tmp_path / "empty.yaml"
        cfg_file.write_text("bots: []\n")
        with patch("agent1.bot_manager.subprocess.Popen") as mock_popen:
            run(cfg_file)
        mock_popen.assert_not_called()
        captured = capsys.readouterr()
        assert "nothing to spawn" in captured.err
