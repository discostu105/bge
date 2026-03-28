# SCO (StarCraft Online) - Game Specification (Reverse Engineered from original game from 2003)

A multiplayer browser-based strategy game inspired by StarCraft. Players choose a race, gather resources, construct buildings, train armies, and compete for territory through combat and diplomacy.

---

## 1. Core Concepts

### 1.1 Game World

- The game runs in **rounds** (ticks). Each round is a fixed time interval (originally 25 minutes).
- All players share the same world. There is no spatial map — players interact by attacking each other directly.
- Each player has a **land** value representing their territory size. Land is the primary measure of power and ranking.
- The game is persistent: rounds process regardless of whether a player is online.

### 1.2 Races

Three playable races, chosen at registration (permanent):

| Race     | ID | Theme                            |
|----------|----|----------------------------------|
| Terran   | 1  | Human military, balanced          |
| Zerg     | 2  | Alien swarm, cheap units          |
| Protoss  | 3  | Advanced aliens, expensive units  |

Each race has its own worker unit, buildings, combat units, and defense structures. The tech trees are asymmetric but balanced.

---

## 2. Resources

### 2.1 Resource Types

| Resource  | Description                                   |
|-----------|-----------------------------------------------|
| Minerals  | Primary resource for buildings and basic units |
| Gas       | Secondary resource for advanced units/upgrades |

### 2.2 Workers

Each race has a worker unit (SCV / Drone / Probe). Workers are **units** that can also be assigned to gather resources. Each worker is assigned to one of three roles:

- **Mineral gathering** (`sondenm`)
- **Gas gathering** (`sondeng`)
- **Idle** (remaining unassigned workers)

Workers can be reassigned freely between roles each round (no cost). Idle workers still count toward the player's forces but produce no resources.

### 2.3 Income per Round

Every round, each player receives:

```
base_income = 10 minerals + 10 gas  (guaranteed minimum)

mineral_efficiency = clamp(land / (mineral_workers * 0.03), 0.2, 100)
gas_efficiency     = clamp(land / (gas_workers * 0.06), 0.2, 100)

minerals_per_worker = 4  (base, scales with land)
gas_per_worker      = 4  (base, scales with land)

mineral_income = mineral_workers * minerals_per_worker * mineral_efficiency / 100
gas_income     = gas_workers * gas_per_worker * gas_efficiency / 100

total_minerals += mineral_income + 10
total_gas      += gas_income + 10
```

**Key design intent:** Efficiency decreases as you assign more workers relative to your land. Players must balance expansion (more land) with worker count to maintain efficient resource gathering. Large empires with few workers are very efficient; small empires with many workers are inefficient.

### 2.4 Resource Trading

Players can trade resources at a **2:1 exchange rate**:
- 2 Gas → 1 Mineral
- 2 Minerals → 1 Gas

### 2.5 Emergency Respawn

If a player has zero workers and very low resources, they automatically receive 1 mineral worker and 1 gas worker to prevent a dead-end state.

---

## 3. Buildings

### 3.1 Building Mechanics

- Buildings are **race-specific**.
- Each building has a **construction time** (in rounds). During construction, the building is non-functional.
- Buildings have **prerequisites** — a building can only be constructed if its prerequisite building is already completed.
- Players can own multiple copies of the same building type.
- One building of each type can be under construction at a time.
- One building per race is the **research building** (marked `RnD`), which enables attack/defense upgrades.

### 3.2 Building Definitions

#### Terran Buildings

| Building                   | Minerals | Gas | Build Time | Prerequisite          | Research |
|----------------------------|----------|-----|------------|-----------------------|----------|
| Command Center             | 400      | 0   | 30         | —                     | No       |
| Barracks                   | 150      | 0   | 10         | Command Center        | No       |
| Armory                     | 100      | 50  | 30         | Factory               | **Yes**  |
| Factory                    | 200      | 100 | 40         | Barracks              | No       |
| Starport                   | 150      | 100 | 40         | Factory               | No       |
| Academy                    | 150      | 0   | 30         | Barracks              | No       |
| Science Facility           | 100      | 150 | 50         | Starport              | No       |

#### Zerg Buildings

| Building                   | Minerals | Gas | Build Time | Prerequisite          | Research |
|----------------------------|----------|-----|------------|-----------------------|----------|
| Hive                       | 400      | 0   | 40         | —                     | No       |
| Spawning Pool              | 150      | 0   | 15         | Hive                  | No       |
| Evolution Chamber          | 75       | 0   | 40         | Spawning Pool         | **Yes**  |
| Hydralisk Den              | 100      | 50  | 20         | Spawning Pool         | No       |
| Ultralisk Cavern           | 150      | 200 | 50         | Spawning Pool         | No       |
| Greater Spire              | 200      | 150 | 60         | Evolution Chamber     | No       |

#### Protoss Buildings

| Building                   | Minerals | Gas | Build Time | Prerequisite          | Research |
|----------------------------|----------|-----|------------|-----------------------|----------|
| Nexus                      | 400      | 0   | 20         | —                     | No       |
| Gateway                    | 150      | 0   | 10         | Nexus                 | No       |
| Forge                      | 200      | 0   | 15         | Gateway               | **Yes**  |
| Cybernetics Core           | 200      | 0   | 18         | Gateway               | No       |
| Robotics Facility          | 200      | 200 | 20         | Forge                 | No       |
| Stargate                   | 150      | 150 | 30         | Robotics Facility     | No       |
| Observatory                | 50       | 100 | 24         | Robotics Facility     | No       |
| Templar Archives           | 150      | 200 | 36         | Stargate              | No       |

### 3.3 Tech Trees (Prerequisite Chains)

**Terran:**
```
Command Center → Barracks → Factory → Armory (Research)
                          → Factory → Starport → Science Facility
                 Barracks → Academy
```

**Zerg:**
```
Hive → Spawning Pool → Evolution Chamber (Research) → Greater Spire
                     → Hydralisk Den
                     → Ultralisk Cavern
```

**Protoss:**
```
Nexus → Gateway → Forge (Research) → Robotics Facility → Stargate → Templar Archives
                                                        → Observatory
                → Cybernetics Core
```

---

## 4. Units

### 4.1 Unit Mechanics

- Units are **trained instantly** (no build time) by spending resources, as long as the required building is completed.
- Units are either **mobile** (can attack) or **stationary** (defense only).
- Mobile units sent to attack have a **return time** (`speed` value in rounds) — they are unavailable for defense while deployed.
- Defense structures (stationary units) cannot attack but defend automatically.
- Workers are also units and participate in combat (with minimal stats).

### 4.2 Unit Groups

- Units are tracked in groups. Each group has a type, count, and position.
- Position 0 = home base (defending). Other positions = attacking a specific player.
- Players can **merge** all units of the same type into one group, or **split** groups for tactical purposes.

### 4.3 Unit Definitions

#### Terran Units

| Unit            | Min  | Gas | Atk | Def | HP  | Speed | Type     | Requires            | Built At     |
|-----------------|------|-----|-----|-----|-----|-------|----------|----------------------|--------------|
| SCV (Worker)    | 50   | 0   | 0   | 1   | 60  | 8     | Mobile   | Command Center       | Cmd Center   |
| Marine          | 45   | 0   | 2   | 4   | 45  | 7     | Mobile   | Barracks             | Barracks     |
| Firebat         | 50   | 25  | 9   | 6   | 50  | 7     | Mobile   | Barracks             | Barracks     |
| Ghost           | 25   | 75  | 12  | 6   | 55  | 7     | Mobile   | Academy              | Barracks     |
| Vulture         | 75   | 0   | 8   | 2   | 70  | 5     | Mobile   | Armory               | Factory      |
| Siege Tank      | 125  | 100 | 10  | 40  | 130 | 9     | Mobile   | Factory              | Factory      |
| Goliath         | 100  | 50  | 8   | 18  | 125 | 7     | Mobile   | Armory               | Factory      |
| Wraith          | 200  | 100 | 36  | 14  | 230 | 5     | Mobile   | Starport             | Starport     |
| Battlecruiser   | 300  | 300 | 70  | 45  | 500 | 11    | Mobile   | Science Facility     | Starport     |
| Missile Turret  | 100  | 0   | 0   | 12  | 135 | —     | **Static** | Academy            | Cmd Center   |

#### Zerg Units

| Unit            | Min  | Gas | Atk | Def | HP  | Speed | Type     | Requires             | Built At     |
|-----------------|------|-----|-----|-----|-----|-------|----------|----------------------|--------------|
| Drone (Worker)  | 50   | 0   | 0   | 1   | 40  | 8     | Mobile   | Hive                 | Hive         |
| Zergling        | 40   | 0   | 3   | 1   | 25  | 6     | Mobile   | Spawning Pool        | Spawn Pool   |
| Hydralisk       | 75   | 50  | 15  | 5   | 80  | 7     | Mobile   | Hydralisk Den        | Hydra Den    |
| Lurker          | 100  | 100 | 12  | 26  | 195 | 10    | Mobile   | Evolution Chamber    | Hydra Den    |
| Ultralisk       | 250  | 200 | 45  | 30  | 450 | 10    | Mobile   | Ultralisk Cavern     | Ultra Cavern |
| Mutalisk        | 200  | 25  | 20  | 26  | 120 | 7     | Mobile   | Greater Spire        | Gr. Spire    |
| Guardian        | 100  | 200 | 50  | 35  | 200 | 12    | Mobile   | Greater Spire        | Gr. Spire    |
| Devourer        | 75   | 225 | 30  | 30  | 295 | 9     | Mobile   | Greater Spire        | Gr. Spire    |
| Sunken Colony   | 175  | 0   | 0   | 24  | 180 | —     | **Static** | Evolution Chamber  | Hive         |

#### Protoss Units

| Unit            | Min  | Gas | Atk | Def | HP  | Speed | Type     | Requires             | Built At     |
|-----------------|------|-----|-----|-----|-----|-------|----------|----------------------|--------------|
| Probe (Worker)  | 50   | 0   | 0   | 1   | 40  | 8     | Mobile   | Nexus                | Nexus        |
| Zealot          | 125  | 0   | 5   | 6   | 130 | 7     | Mobile   | Gateway              | Gateway      |
| Dragoon         | 150  | 50  | 12  | 18  | 180 | 7     | Mobile   | Cybernetics Core     | Gateway      |
| Archon          | 120  | 300 | 28  | 42  | 380 | 8     | Mobile   | Forge                | Gateway      |
| Dark Templar    | 125  | 100 | 30  | 30  | 80  | 6     | Mobile   | Templar Archives     | Temp Archive |
| Reaver          | 275  | 100 | 65  | 0   | 160 | 12    | Mobile   | Robotics Facility    | Robotics     |
| Observer        | 25   | 25  | 0   | 0   | 20  | 4     | Mobile   | Observatory          | Robotics     |
| Scout           | 300  | 150 | 28  | 49  | 310 | 6     | Mobile   | Stargate             | Stargate     |
| Carrier         | 550  | 300 | 80  | 55  | 600 | 12    | Mobile   | Stargate             | Stargate     |
| Photon Cannon   | 150  | 0   | 0   | 20  | 200 | —     | **Static** | Forge              | Nexus        |

### 4.4 Upgrade Bonuses per Unit

Each unit has per-level bonuses for attack upgrades (a1/a2/a3) and defense upgrades (d1/d2/d3). These are added to the unit's base stats when the player has researched the corresponding upgrade level.

**Example:** A Marine with base Atk=2 and a1=1, a2=1, a3=2 would deal:
- No upgrades: 2 damage
- Attack Upgrade 1: 2 + 1 = 3 damage
- Attack Upgrade 2: 2 + 1 = 3 damage (the a2 value is the TOTAL bonus at level 2, not cumulative — see section 5)
- Attack Upgrade 3: 2 + 2 = 4 damage

Workers and defense structures have 0 attack bonuses but do receive defense bonuses (d1/d2/d3).

**Full upgrade bonus table (from init-data):**

| Unit            | a1 | a2 | a3 | d1 | d2 | d3 |
|-----------------|----|----|----|----|----|-----|
| SCV             | 0  | 0  | 0  | 0  | 0  | 0   |
| Marine          | 1  | 1  | 2  | 1  | 2  | 3   |
| Firebat         | 1  | 2  | 3  | 1  | 2  | 3   |
| Ghost           | 1  | 2  | 3  | 1  | 2  | 3   |
| Vulture         | 1  | 2  | 3  | 1  | 1  | 2   |
| Siege Tank      | 1  | 2  | 3  | 2  | 4  | 6   |
| Goliath         | 1  | 2  | 3  | 2  | 4  | 6   |
| Wraith          | 2  | 4  | 6  | 1  | 2  | 3   |
| Battlecruiser   | 4  | 8  | 12 | 2  | 4  | 6   |
| Missile Turret  | 0  | 0  | 0  | 2  | 4  | 6   |
| Drone           | 0  | 0  | 0  | 0  | 0  | 0   |
| Zergling        | 1  | 1  | 2  | 1  | 1  | 1   |
| Hydralisk       | 1  | 2  | 3  | 1  | 1  | 2   |
| Lurker          | 1  | 1  | 2  | 2  | 4  | 6   |
| Ultralisk       | 2  | 4  | 6  | 2  | 4  | 6   |
| Mutalisk        | 1  | 2  | 3  | 2  | 4  | 6   |
| Guardian        | 4  | 8  | 12 | 2  | 4  | 6   |
| Devourer        | 2  | 4  | 6  | 2  | 4  | 6   |
| Probe           | 0  | 0  | 0  | 0  | 0  | 0   |
| Zealot          | 1  | 2  | 3  | 1  | 2  | 3   |
| Dragoon         | 1  | 2  | 3  | 1  | 2  | 3   |
| Archon          | 2  | 4  | 6  | 2  | 4  | 6   |
| Dark Templar    | 2  | 4  | 6  | 2  | 4  | 6   |
| Reaver          | 4  | 8  | 12 | 0  | 0  | 0   |
| Observer        | 0  | 0  | 0  | 0  | 0  | 0   |
| Scout           | 2  | 4  | 6  | 4  | 8  | 12  |
| Carrier         | 4  | 8  | 12 | 4  | 8  | 12  |
| Photon Cannon   | 0  | 0  | 0  | 2  | 4  | 6   |
| Sunken Colony   | 0  | 0  | 0  | 2  | 4  | 6   |

---

## 5. Upgrades (Research)

### 5.1 Upgrade Mechanics

- Upgrades require the race's **research building** to be completed.
- Only **one upgrade** can be researching at a time (attack and defense share the queue).
- Upgrades take multiple rounds to complete (countdown timer).
- Attack upgrades have 3 levels. Defense upgrades have 3 levels (internally stored as levels 4-6).

### 5.2 Upgrade Definitions

| Upgrade            | Minerals | Gas | Bonus | Research Time (rounds) |
|--------------------|----------|-----|-------|------------------------|
| Attack Upgrade 1   | 50       | 100 | +1    | 20                     |
| Attack Upgrade 2   | 150      | 200 | +2    | 40                     |
| Attack Upgrade 3   | 400      | 400 | +3    | 50                     |
| Defense Upgrade 1  | 200      | 200 | +1    | 20                     |
| Defense Upgrade 2  | 300      | 300 | +2    | 40                     |
| Defense Upgrade 3  | 400      | 500 | +3    | 50                     |

### 5.3 How Upgrades Apply to Combat

When a player has attack upgrade level N (1-3), each unit's attack stat gains the unit's `a[N]` bonus.
When a player has defense upgrade level N (1-3), each unit's defense stat gains the unit's `d[N]` bonus.

The bonus values (a1-a3, d1-d3) are per-unit-type and are **not cumulative** — only the bonus for the current level applies.

---

## 6. Combat System

### 6.1 Attack Restrictions

A player **cannot attack** a target if:
- The attacker has `protected > 0` (still under new-player protection)
- The defender has `protected > 0`
- The defender's land is less than 50% of the attacker's land (prevents bullying)
- The attacker and defender are in the same alliance

### 6.2 Deploying Troops

1. The attacker selects a target player.
2. The attacker sends **mobile units** (in groups) toward the target. Sent troops leave the player's defense.
3. Troops arrive instantly for the attack. After combat, surviving attacking troops take `speed` rounds to return home.
4. While returning, troops are unavailable for defense.
5. Additional reinforcements can be sent before the final attack is triggered.

### 6.3 Battle Resolution

Combat is resolved in **8 iterations** (loops). Each iteration consists of:

**Step A — Attacker Strikes:**
1. Calculate total attacker damage: `sum( (unit_attack + upgrade_bonus) * unit_count )` for all attacking units.
2. Apply damage to defender's units, targeting **lowest-HP unit types first**.
3. Destroyed units are removed. Overflow damage carries to the next unit type.

**Step B — Defender Counter-Strikes:**
1. Calculate total defender damage: `sum( (unit_defense + upgrade_bonus) * unit_count )` for all remaining defending units (including static defenses).
2. Apply damage to attacker's units, same priority (lowest HP first).
3. Destroyed units are removed.

This repeats for 8 iterations. The battle ends early if either side has no units remaining.

**Important notes:**
- Attackers use their **attack** stat. Defenders use their **defense** stat.
- Static defense structures (Missile Turret, Sunken Colony, Photon Cannon) have 0 attack but contribute their defense stat.
- Workers participate in combat with their minimal stats.

### 6.4 Victory and Land Transfer

**Attacker wins** if they have surviving units and the defender has none.

Land gained on victory:
```
percent = (attacker_remaining_damage / defender_land) * 12
percent = clamp(percent, 1, 50)
land_gained = floor(defender_land * percent / 100)

attacker.land += land_gained
defender.land -= land_gained
```

The attacker also captures a portion of the defender's workers:
```
total_probes = defender.mineral_workers + defender.gas_workers
captured = round((total_probes * percent / 100) / 2)
// Captured probes are removed from defender (not added to attacker)
```

### 6.5 Battle Reports

Both attacker and defender receive a detailed battle report via the messaging system, listing:
- Forces involved on each side
- Losses for each unit type
- Land gained/lost
- Final outcome

---

## 7. Colonization

Players can expand their territory by spending minerals:

```
cost_per_land = max(1, floor(current_land / 4))
max_colonize = 24 land per action
total_cost = amount * cost_per_land
```

This is a peaceful alternative to combat for gaining land. As a player's land grows, colonization becomes increasingly expensive.

---

## 8. New Player Protection

- New players receive a **protection timer** (approximately 32 rounds).
- While `protected > 0`, the player cannot be attacked and cannot attack others.
- The timer decrements by 1 each round.
- Protection status is visible in the ranking.

---

## 9. Alliance System

### 9.1 Alliance Basics

- Players can create or join an alliance.
- Joining requires the alliance password.
- New members start as pending and must be accepted.
- Members of the same alliance cannot attack each other.
- Minimum land is maintained at 1 (a player can never be completely eliminated).

### 9.2 Alliance Leadership

- The member with the most **votes** from other alliance members becomes the leader.
- The leader can:
  - Change the alliance password
  - Set an alliance message/announcement
  - Set an alliance logo URL
  - Kick members
  - Accept/reject pending members

### 9.3 Alliance Features

- **Alliance chat** — separate channel for alliance members only
- **Alliance view** — members can optionally share detailed stats (troops, resources, land) with allies
- **Alliance ranking** — alliances are ranked by a formula: `(average_member_land + total_member_land / 12)`

---

## 10. Communication

### 10.1 Private Messages

- Players can send and receive messages to/from other players.
- Messages have a subject and body.
- Unread message count is displayed in the UI header.
- Battle reports are delivered as messages.

### 10.2 Chat

- Real-time chat with two modes: public and alliance-only.
- Chat auto-refreshes to show new messages.

---

## 11. Build Queue (Todo System)

Players can queue future actions:
- Queue building construction
- Queue unit training
- Set priority order (reorder items)

The queue auto-executes when prerequisites and resources are available during the game tick.

---

## 12. Ranking

### 12.1 Player Ranking

- Players are ranked by **land owned** (descending).
- Tiebreaker: alphabetical by username.
- The ranking shows: rank, username, race, land, alliance, online/offline status, protection status.

### 12.2 Alliance Ranking

- Alliances ranked by: `average_member_land + total_member_land / 12`
- Only alliances with 2+ members are shown.
- Shows: alliance rank, name, member count, total land, average land.

### 12.3 Online Status

- A player is considered online if their last activity was within the last ~8 minutes.
- Otherwise they are shown as offline.

---

## 13. Game Tick (Round Processing)

Each round (every 25 minutes), the server processes the following for **every active player**:

1. **Decrement protection timer** (`protected -= 1` if > 0)
2. **Ensure minimum land** (land forced to 1 if below)
3. **Decrement troop return timers** (troops returning from attacks)
4. **Decrement building construction timers** (buildings under construction)
5. **Process upgrade completion:**
   - If attack upgrade timer reaches 0 → increment attack upgrade level
   - If defense upgrade timer reaches 0 → increment defense upgrade level
6. **Calculate and add resource income** (see Section 2.3)
7. **Process build queue** (auto-build queued items if affordable)
8. **Increment round counter** for the player

The tick uses a global lock to prevent concurrent processing.

---

## 14. User Accounts

### 14.1 Registration

- Username (unique), login name, password, email address
- Race selection (permanent)
- Multi-account detection warning

### 14.2 Player State

Key per-player fields:

| Field             | Description                                |
|-------------------|--------------------------------------------|
| land              | Territory owned (primary ranking metric)   |
| minerals          | Current mineral reserves                    |
| gas               | Current gas reserves                        |
| mineral_workers   | Workers assigned to mineral gathering       |
| gas_workers       | Workers assigned to gas gathering            |
| race              | 1=Terran, 2=Zerg, 3=Protoss                |
| alliance          | Alliance membership (0 = none)              |
| attack_upgrade    | Current attack upgrade level (0-3)          |
| defense_upgrade   | Current defense upgrade level (0-3, stored as 0 or 4-6) |
| attack_timer      | Rounds remaining for attack upgrade research |
| defense_timer     | Rounds remaining for defense upgrade research |
| protected         | Rounds of new-player protection remaining    |
| round             | Total rounds played                          |
| last_online       | Last activity timestamp                      |

---

## 15. Pages / Feature Set

The game UI consists of these functional areas:

| Page              | Purpose                                                  |
|-------------------|----------------------------------------------------------|
| Login/Register    | Account creation with race selection, authentication     |
| Overview          | Dashboard showing player stats, game guide               |
| Resources         | Allocate workers to mining, trade resources               |
| Base              | Construct buildings, manage tech tree, research upgrades  |
| Troops            | View/manage units, merge/split groups                     |
| Attack            | Select target, deploy troops, execute attack              |
| Colonize          | Spend minerals to expand territory                        |
| Alliance          | Create/join/manage alliance, vote for leader              |
| Alliance Info     | Public view of alliance members and stats                 |
| Alliance View     | Detailed member stats (alliance-only, opt-in)             |
| Ranking           | Player leaderboard sorted by land                         |
| Alliance Ranking  | Alliance leaderboard                                      |
| Unit Reference    | Stat sheet for all units across all races                  |
| Messages          | Send/receive private messages and battle reports           |
| Chat              | Real-time public and alliance chat                         |
| Build Queue       | Manage queued building and training orders                 |
| Admin             | Round management, server statistics                        |

### 15.1 UI Header (Persistent)

Always visible:
- Player username
- Current minerals and gas
- Current land
- Time until next round
- Unread message count
- Server time

---

## 16. Data Model Summary

### Core Game Tables

```
players
  id, username, login_name, password_hash, email, race,
  land, minerals, gas, mineral_workers, gas_workers,
  alliance_id, alliance_accepted, alliance_message,
  attack_upgrade, defense_upgrade, attack_timer, defense_timer,
  protected, round, last_online, last_update

alliances
  id, name, password, message, logo_url

unit_types  (static reference data)
  id, name, minerals, gas, attack, defense, hitpoints,
  race, requires_building, built_at_building,
  attack_bonus_1, attack_bonus_2, attack_bonus_3,
  defense_bonus_1, defense_bonus_2, defense_bonus_3,
  speed, is_mobile

building_types  (static reference data)
  id, name, minerals, gas, race, build_time,
  requires_building, is_research

upgrades  (static reference data)
  id, name, minerals, gas, bonus, research_time

units  (player's army)
  id, player_id, unit_type_id, count, position, return_timer

buildings  (player's constructed buildings)
  id, player_id, building_type_id, construction_remaining

build_queue
  id, player_id, action_type, target_type_id, quantity, priority

messages
  id, sender_id, recipient_id, subject, body, timestamp, read

game_state  (singleton)
  last_update, current_round, is_updating
```

---

## 17. Design Notes for Re-implementation

### What to Keep
- The core economic loop: gather → build → train → fight → expand
- Asymmetric races with distinct tech trees
- The land-as-power metric and efficiency scaling
- The tension between expansion and worker efficiency
- Alliance politics (voting, shared visibility, mutual protection)
- Real-time ticks processing the game world
- Battle reports as an important feedback mechanism

### What to Modernize
- Replace classic ASP/Access with a modern web stack
- Use proper authentication (hashed passwords, sessions/JWT)
- Use WebSockets for real-time chat and notifications instead of iframe polling
- Use a proper relational database (PostgreSQL, etc.)
- Make round timing configurable
- Add a proper REST or GraphQL API to separate frontend from backend
- The integrated MBBS forum can be dropped — use Discord/external community tools
- The photo album, calendar, and other forum features are unrelated to the game and should be omitted

### Balance Considerations
- The 50% land restriction on attacking prevents extreme bullying but could be tuned
- High-tier units (Battlecruiser, Carrier, Guardian) have enormous stat advantages — ensure the cost and tech-tree depth justifies them
- Defense structures have 0 attack but strong defense — they only help when being attacked
- The 2:1 trade ratio is intentionally unfavorable to prevent resource gaming
- Protection period (32 rounds) gives new players time to build up
