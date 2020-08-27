# Browser Game Engine

It's a game engine for simple browser games. This is a hobby project.

## Game properties

 * A player has resources, assets (buildings) and units
 * At every game tick (turn), resources grow
 * The goal is to maximize the primary resource (e.g. land) and make it to the top of the player-ranking.

## Architecture goals

 * Keep it simple. Simplicity over scalability. This is meant to be a fun/hobby coding project. Not a hugely scalable MMO.
 * Make it generic enough to use the game engine for different game definitions (different names, graphics, game logic to some degree).

 ### Design/Tech

 * Uses .NET 5 & Blazor WebAssembly as Single-Page-App.
 * Uses Discord as OAuth2 provider.
 * The game itself runs in a stateful monolith. All game state is kept in memory. To persist the game-state, it's simply serialized as a blob to some storage. This shall happen on shutdown, but also periodically to have a "backup" in case of an application crash. That design is (hopefully) simpler than using a relational (or any other) database.

## History

In 2003, one of my first coding projects was a browser based game, called "StarCraft Online". It was played intensively by a few hundred people back then and was kind of addictive & fun. I made a few revivals of the game over the years, but never actually develped it further. It was based on ASP/VBScript and was not really well coded.

Now, in 2020, I got a taste of browser games again and wanted to revive my old game - but differently this time. I want to re-write the entire game from scratch, using modern technologies, while keeping the game logic as close to the original as possible (at least in the first iteration). Also, I wanted to do this open-source from the beginning.