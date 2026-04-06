import pytest
from agent1.strategy.difficulty import (
    DifficultyProfile,
    EASY,
    MEDIUM,
    HARD,
    EXPERT,
    PROFILES,
)
from agent1.strategy.context import StrategyContext
from agent1.strategy.military import should_attack
from agent1.strategy.resources import worker_target, should_expand
from agent1.config import AgentConfig


# ---------------------------------------------------------------------------
# DifficultyProfile dataclass
# ---------------------------------------------------------------------------

class TestDifficultyProfile:
    def test_profiles_are_frozen(self):
        with pytest.raises((AttributeError, TypeError)):
            EASY.aggression = 0.9  # type: ignore[misc]

    def test_all_profiles_have_valid_ranges(self):
        for profile in PROFILES.values():
            assert 0.0 <= profile.aggression <= 1.0
            assert 0.0 <= profile.resource_priority <= 1.0
            assert 0.0 <= profile.expansion_threshold <= 1.0

    def test_profiles_are_ordered_by_difficulty(self):
        assert EASY.aggression < MEDIUM.aggression < HARD.aggression < EXPERT.aggression
        assert EASY.resource_priority < MEDIUM.resource_priority < HARD.resource_priority < EXPERT.resource_priority
        # expansion threshold decreases as difficulty rises
        assert EASY.expansion_threshold > MEDIUM.expansion_threshold > HARD.expansion_threshold > EXPERT.expansion_threshold

    def test_profiles_lookup_dict_contains_all_four(self):
        assert set(PROFILES.keys()) == {"easy", "medium", "hard", "expert"}

    def test_profiles_lookup_returns_correct_objects(self):
        assert PROFILES["easy"] is EASY
        assert PROFILES["medium"] is MEDIUM
        assert PROFILES["hard"] is HARD
        assert PROFILES["expert"] is EXPERT

    def test_custom_profile_creation(self):
        custom = DifficultyProfile(
            name="custom",
            aggression=0.5,
            resource_priority=0.5,
            expansion_threshold=0.5,
        )
        assert custom.name == "custom"
        assert custom.aggression == 0.5


# ---------------------------------------------------------------------------
# StrategyContext defaults
# ---------------------------------------------------------------------------

class TestStrategyContext:
    def test_default_difficulty_is_medium(self):
        ctx = StrategyContext()
        assert ctx.difficulty_profile is MEDIUM

    def test_land_ratio_zero_when_no_land(self):
        ctx = StrategyContext(land=0, max_land=0)
        assert ctx.land_ratio == 0.0

    def test_land_ratio_calculation(self):
        ctx = StrategyContext(land=50, max_land=100)
        assert ctx.land_ratio == pytest.approx(0.5)


# ---------------------------------------------------------------------------
# Military strategy — should_attack
# ---------------------------------------------------------------------------

class TestMilitary:
    def test_easy_does_not_attack_equal_forces(self):
        ctx = StrategyContext(units=10, difficulty_profile=EASY)
        assert not should_attack(ctx, enemy_units=10)

    def test_easy_attacks_when_heavily_outnumbering(self):
        ctx = StrategyContext(units=100, difficulty_profile=EASY)
        assert should_attack(ctx, enemy_units=10)

    def test_expert_attacks_with_parity(self):
        ctx = StrategyContext(units=10, difficulty_profile=EXPERT)
        # required_ratio at expert = 5.0 - 4.2*1.0 = 0.8
        assert should_attack(ctx, enemy_units=10)

    def test_always_attack_when_no_defenders(self):
        for profile in PROFILES.values():
            ctx = StrategyContext(units=1, difficulty_profile=profile)
            assert should_attack(ctx, enemy_units=0)


# ---------------------------------------------------------------------------
# Resource strategy — worker_target, should_expand
# ---------------------------------------------------------------------------

class TestResources:
    def test_worker_target_scales_with_resource_priority(self):
        easy_ctx = StrategyContext(difficulty_profile=EASY)
        expert_ctx = StrategyContext(difficulty_profile=EXPERT)
        assert worker_target(easy_ctx, max_workers=100) < worker_target(expert_ctx, max_workers=100)

    def test_worker_target_never_zero(self):
        ctx = StrategyContext(difficulty_profile=EASY)
        assert worker_target(ctx, max_workers=1) >= 1

    def test_easy_does_not_expand_at_half_fill(self):
        ctx = StrategyContext(land=50, max_land=100, difficulty_profile=EASY)
        assert not should_expand(ctx)

    def test_expert_expands_at_half_fill(self):
        # EXPERT threshold is 0.3, so 50% fill triggers expansion
        ctx = StrategyContext(land=50, max_land=100, difficulty_profile=EXPERT)
        assert should_expand(ctx)


# ---------------------------------------------------------------------------
# AgentConfig
# ---------------------------------------------------------------------------

class TestAgentConfig:
    def test_default_is_medium(self):
        cfg = AgentConfig()
        assert cfg.difficulty_profile is MEDIUM

    def test_case_insensitive_lookup(self):
        cfg = AgentConfig(difficulty="HARD")
        assert cfg.difficulty_profile is HARD

    def test_invalid_difficulty_raises(self):
        with pytest.raises(ValueError, match="Unknown difficulty"):
            AgentConfig(difficulty="impossible")

    @pytest.mark.parametrize("name,expected", [
        ("easy", EASY),
        ("medium", MEDIUM),
        ("hard", HARD),
        ("expert", EXPERT),
    ])
    def test_all_profiles_resolve(self, name, expected):
        cfg = AgentConfig(difficulty=name)
        assert cfg.difficulty_profile is expected
