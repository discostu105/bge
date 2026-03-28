# Auth Plan: GitHub + API Keys

## Goal

Replace Discord OAuth with GitHub OAuth. Decouple User from Player so one person can own multiple players (human, AI agent, or both). Make it trivial for agent builders to authenticate via API key.

## Data Model

```
User
  id:             GUID
  github_id:      string (unique)
  github_login:   string
  display_name:   string
  created:        timestamp

Player
  id:             PlayerId
  user_id:        → User.id
  name:           string
  api_key_hash:   string?       (null = no API access)
  created:        timestamp
```

No roles, no permissions. A User owns Players. A Player optionally has an API key.

## Authentication

### Human (browser)

- GitHub OAuth via `AddGitHub()` → cookie session
- On first login, create User record from GitHub claims
- Player selection via UI (session or `X-Player-Id` header)

### Agent (API)

- `Authorization: Bearer bge_k_...` → hash key → look up Player → authenticated
- No OAuth, no token refresh, no callback URLs

### Middleware

```
Request
  ├── Cookie? → resolve User → resolve Player (from session/header) → verify ownership
  └── Bearer bge_k_...? → hash → look up Player directly
Both paths → CurrentUserContext with PlayerId (game code unchanged)
```

## UI Changes

- `/signin` — GitHub button only (remove Discord)
- `/players` — list my players, create new, delete
- `/players/{id}/apikey` — generate key (shown once), revoke key
- Leaderboard — group by User, filter by human/agent

## API for Agent Builders

```bash
# Get game state
curl -H "Authorization: Bearer bge_k_..." /api/worldstate

# Take action
curl -X POST -H "Authorization: Bearer bge_k_..." /api/battle/attack?targetPlayerId=...
```

Expose OpenAPI spec at `/openapi/v1.json` in production (already available in dev).

## Migration Steps

1. Add User entity (in-memory + blob persistence, same as existing game state)
2. Replace Discord OAuth with GitHub OAuth
3. Decouple Player from auth identity — Player gets own ID, linked to User
4. Add player management UI (create/delete/rename players)
5. Add API key generation (hash with SHA-256, store hash only)
6. Add Bearer token auth middleware alongside cookie auth
7. Add rate limiting per API key
8. Expose OpenAPI spec in prod
9. Add leaderboard page

## What We're Not Doing

- No Microsoft/Google/Discord — GitHub only, keeps it simple
- No JWT — cookie + API key is enough for a game with in-memory state
- No OAuth client credentials for agents — API keys are simpler
- No RBAC — "you own what you created" is the only rule
