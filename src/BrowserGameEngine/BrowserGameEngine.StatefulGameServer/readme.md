This layer must be atomic in every action.

DON'T: read state, then do calcuation, then store.

DO: Issue a command message that does everything atomically.