# Changelog

Notable changes to the game, newest first. The format follows [Keep a Changelog](https://keepachangelog.com/),
and versions follow [Semantic Versioning](https://semver.org/). Each released section is what the release
workflow publishes as that release's notes, so write it for players.

## [Unreleased]

### Added

- Realistic shaders, switched on and off in `Options` → `Shaders` while playing: light from the sun and moon
  where they really are, warm sunsets and cool nights, soft shadows, leaves and plants that sway, rippling water
  that reflects the sky and catches the sun, a sky with a sun glow, moon and stars, and bloom.
- A reworked interface: one consistent look across every screen, smooth hover and selection, and a scale that
  follows the window size, with a `GUI Scale` option on top.
- Every menu can be used from the keyboard: `Tab` and the arrows move, `Enter` confirms, `Escape` goes back.
- Text boxes and the chat support selection, word-at-a-time editing and the clipboard.
- The world list shows each world's game mode, when it was last played and its seed; double click plays,
  `F2` renames and `Delete` deletes.
- Item tooltips in the inventory with durability, damage and how much food fills.
- Hearts and hunger drawn as pixel art with halves, flashing when hurt and pulsing when low.
- A `Show FPS` option, and notices for things like the world being saved.
- The version is shown on the title screen and in the window title.
- Releases for Windows on the GitHub Releases page, with and without a bundled .NET runtime, and checksums.

### Changed

- Worlds and options are kept in `%APPDATA%\MinecraftClone`, so a new version can be unzipped anywhere and
  find them. A copy that already keeps its worlds beside the executable carries on doing so.
- Pausing a hosted world saves it.
- Deleting a world puts `Keep It` first and holds `Delete Forever` back for a moment.
- The game asks for OpenGL 4.0, which its shaders always needed.

### Fixed

- Losing the connection to a server, or being refused by one, returns to the title screen with the reason
  instead of leaving a frozen world.
- A block the server turned down after accepting it, because a mob stepped into the space or another player
  placed there first, no longer takes the item with it.
- A hit wears the weapon that swung rather than whatever is held when the server confirms it, and a slow
  break no longer wears a tool twice for one block.
- Creative players can use chests and furnaces at creative reach.
- Player names are cleaned of control characters and capped at sixteen characters.
- Generating terrain while a player joined or respawned could give chunks the wrong biome or height.
- A corrupt time of day in a save no longer crashes the game.
- Worlds named after Windows device names such as `con` or `nul` can be saved.
- Oak trees near the top of the world no longer crash the generator.
- A missing or broken audio device no longer leaks the half-opened output.
