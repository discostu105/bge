# Bot Client Guide

This guide explains how to build an automated bot player for BGE using the REST API.

## Prerequisites

- A BGE account with an active player in a game
- Basic familiarity with HTTP and your language of choice
- A working example is provided in [`docs/bot-client-example/bot.py`](bot-client-example/bot.py)

---

## Step 1: Get an API Key

1. Log in to BGE and navigate to the **Player Management** page.
2. Click **Generate API Key** next to your player.
3. Copy the key — it starts with `bge_k_` and is only shown once.
4. Store it securely (environment variable, secrets manager, etc.).

You can also generate a key programmatically once you have an existing authenticated session:

```
POST /api/players/{playerId}/apikey
Authorization: Bearer <session-token>
```

Response:
```json
{ "apiKey": "bge_k_..." }
```

---

## Step 2: Authenticate

Include the API key as a Bearer token on every request:

```
Authorization: Bearer bge_k_<your-key>
```

All game endpoints require authentication. A `401 Unauthorized` means the key is invalid or missing.

---

## Step 3: The Game Loop

BGE runs on a **tick-based** simulation. The server advances game state every ~10 seconds. Your bot should:

1. **Poll `/api/game/tick-info`** to find out when the next tick fires.
2. **Wait** until shortly after the tick time.
3. **Read current state** (resources, workers, units, rankings).
4. **Issue commands** for the new tick (assign workers, build assets/units, attack).
5. Repeat.

```python
while True:
    tick_info = get("/api/game/tick-info")
    sleep_until(tick_info["nextTickAt"])
    read_state_and_issue_commands()
```

Tick info response:
```json
{
  "serverTime":   "2026-03-30T12:00:00Z",
  "nextTickAt":   "2026-03-30T12:00:10Z",
  "unreadMessageCount": 0
}
```

---

## Useful Endpoints

### Resources
`GET /api/resources` — current resource counts (minerals, gas, etc.)

```json
{
  "primaryResource": { "resourceDefId": "minerals", "amount": 1500 },
  "secondaryResources": [{ "resourceDefId": "gas", "amount": 240 }]
}
```

`POST /api/resources/Trade?fromResource=minerals&amount=500` — trade one resource for another.

---

### Workers
`GET /api/workers` — current worker assignments

```json
{ "totalWorkers": 10, "mineralWorkers": 7, "gasWorkers": 3 }
```

`POST /api/workers/Assign?mineralWorkers=8&gasWorkers=2` — reassign all workers.

Workers are the primary income driver. Prioritise keeping them all assigned.

---

### Assets (Buildings)
`GET /api/assets` — list of built and queued assets

`POST /api/assets/Build?assetDefId=commandcenter` — queue a building.

Building IDs depend on the active game definition. Read `/api/assets` to see what's available and what's already built.

---

### Units (Military)
`GET /api/units` — list of unit stacks

```json
{ "units": [{ "unitId": "...", "unitDefId": "marine", "count": 12 }] }
```

`POST /api/units/Build?unitDefId=marine&count=5` — train units.

`POST /api/units/Merge` — merge all same-type unit stacks into one.

`POST /api/units/Split?unitId=<guid>&splitCount=6` — split a stack.

---

### Attacking
`GET /api/battle/AttackablePlayers` — list of players you can attack

`GET /api/battle/EnemyBase?enemyPlayerId=<id>` — scout enemy defences before committing

`POST /api/battle/SendUnits?unitId=<guid>&enemyPlayerId=<id>` — move units toward a target

`POST /api/battle/Attack?enemyPlayerId=<id>` — resolve the attack

---

### Rankings
`GET /api/playerranking` — all players sorted by rank; use this to identify the weakest target.

---

## Tips

- **Don't spam requests.** Sleep until after the next tick; issuing commands mid-tick wastes CPU and may be rate-limited in the future.
- **Workers first.** Idle workers mean zero income. Always keep them fully assigned.
- **Build before training.** Most unit types require specific buildings. Check `/api/assets` before queuing units.
- **Scout before attacking.** `GET /api/battle/EnemyBase` is free — use it to avoid suiciding units into a heavily defended base.
- **Handle 400 gracefully.** Commands can fail (insufficient resources, wrong game state). Log the error body and continue.

---

## Complete Example

See [`docs/bot-client-example/bot.py`](bot-client-example/bot.py) for a fully commented Python bot (~150 lines) that:

- Authenticates with an API key
- Polls tick-info for the game loop
- Keeps all workers assigned to minerals
- Queues buildings when affordable
- Trains marines
- Attacks the lowest-ranked player each tick
