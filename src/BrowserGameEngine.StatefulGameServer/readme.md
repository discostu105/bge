﻿This layer must be atomic in every action.

DON'T: read state, then do calcuation, then store.
DO: Issue a command message that does everything atomically.

In here, the whole game-state is kept in memory. It shall be persistet as a whole blob for persistence periodically.
That design is chosen for simplicity, not for infinite scalability.

The API's in this layer are not aware of current player context, so each command must contain the player id.

This layer must never return the original objects. They must be immutable copies. (This rule is violated for now to simplify development).