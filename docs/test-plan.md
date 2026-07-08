# Manual Test Plan

## Opening and Ending Chapters

1. Create or pick an episode with `opening`, `A part`, `B part`, and `ending` chapters.
2. Play with a fresh user account.
3. Confirm `opening` and `ending` are not skipped on first playback.
4. Play the same episode again.
5. Confirm `opening` and `ending` seek to their end.
6. Confirm `A part` and `B part` are never skipped.

## User Isolation

1. Use user A to watch at least 70% of the opening.
2. Replay as user A and confirm the opening skips.
3. Replay as user B and confirm the opening does not skip.

## Existing Intro Marker

1. Use an item with Emby/StrmAssistant intro marker data.
2. Confirm the marker is handled like an opening segment.
3. Confirm ending is not skipped unless an ending marker or chapter exists.

## Switches

1. Disable the plugin and confirm no seek occurs.
2. Enable only opening skip and confirm endings are not skipped.
3. Enable only ending skip and confirm openings are not skipped.
