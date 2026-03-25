# 7D2D Titles System

A **server-side** mod for **7 Days to Die** (v2.5) that gives players apocalyptic rank titles based on zombie kills. Titles are displayed above each player's model and announced in chat on rank-up.

## Features

- 🧟 **30 progressive ranks** based on zombie kills — from *Freshly Irradiated Civilian* to *Last Hope of Humanity*
- 🪧 **Name display** — short rank title shown in brackets above each player's model (`[Warlord] PlayerName`)
- 📢 **Rank-up announcements** — global server chat notification when a player levels up
- 💾 **Persistent data** — kill counts and ranks survive server restarts, stored per world save
- 📊 **Player statistics** — track deaths, kill streaks, playtime, K/D ratio, kills/hour, and weapon usage
- 🛠️ **Admin commands** — check ranks, force-set kills, view leaderboards, and inspect detailed player stats
- ⚙️ **Fully configurable** — add, remove, or rename ranks in `Config/TitlesRanks.xml` without recompiling
- 🏆 **Automatic leaderboards** — global leaderboard displayed on player login and periodically (configurable interval)
- 🔒 **Server-side only** — no client-side installation required

## Installation

1. Download the latest release or [build from source](#building-from-source).
2. Copy `TitlesSystem/` (containing `ModInfo.xml`, `TitlesSystem.dll`, and `Config/TitlesRanks.xml`) into your server's `Mods/` directory.
3. Restart the server.

## Configuration

Edit `Config/TitlesRanks.xml` to customise settings and ranks. Changes take effect on the next server restart.

### Core Settings
```xml
```xml
<Settings>
    <ShowRankInName value="true"/>              <!-- show rank above player head -->
    <AnnounceRankUp value="true"/>              <!-- broadcast rank-up in chat -->
    <AutoSaveIntervalMinutes value="5"/>        <!-- 0 = save on disconnect only -->
    <ShowLeaderboardOnLogin value="true"/>      <!-- show leaderboard when players join -->
    <ShowLeaderboardIntervalHours value="6"/>   <!-- show leaderboard every N in-game hours (0 = disabled) -->
    <LeaderboardTopPlayers value="10"/>         <!-- number of top players to display -->
</Settings>
```
<Ranks>
### Leaderboard Settings
    <!-- ranks must be ordered by ascending kills value -->
| Setting | Default | Description |
|---------|---------|-------------|
| `ShowLeaderboardOnLogin` | `true` | Display the top players leaderboard in global chat when any player joins |
| `ShowLeaderboardIntervalHours` | `6` | Periodically broadcast leaderboard every N in-game hours (set to 0 to disable periodic broadcasts) |
| `LeaderboardTopPlayers` | `10` | Number of top players to show in leaderboard rankings |
    <Rank kills="0"   title="Your Custom Rank" shortTitle="Custom"/>
The leaderboard displays each player's rank position, name, total zombie kills, and current rank title. It updates automatically throughout the server's lifetime.
    <Rank kills="100" title="Another Rank"      shortTitle="Another"/>
### Rank Configuration
```
```xml
<Ranks>

## Ranks

</Ranks>
```
| 1 | `Civilian` | Freshly Irradiated Civilian | 0 |
| 2 | `Diver` | Dumpster Diver of Doom | 5 |
| 3 | `Rusty` | Rusty Nail Enthusiast | 10 |
| 4 | `CanOpener` | Can Opener Connoisseur | 20 |
| 5 | `TinCan` | Tin Can Knight | 35 |
| 6 | `Wanderer` | Wandering Wastelander | 50 |
| 7 | `Drifter` | Dead Road Drifter | 75 |
| 8 | `Scavenger` | Scavenger of the Fallen | 100 |
| 9 | `Prophet` | Junkyard Prophet | 130 |
| 10 | `Raider` | Honorary Raider | 175 |
| 11 | `VaultKicker` | Vault Door Kickboxer | 250 |
| 12 | `Buster` | Bunker Buster | 300 |
| 13 | `Whisperer` | Mutant Whisperer | 375 |
| 14 | `LastShell` | The Last Shotgun Shell | 500 |
| 15 | `Baron` | Bottle Cap Baron | 600 |
| 16 | `Duke` | Duke of the Dead Lands | 750 |
| 17 | `Headliner` | Horde Night Headliner | 1,250 |
| 18 | `Trapper` | Ghoul Trapper | 1,500 |
| 19 | `Ambassador` | Ambassador of Annihilation | 2,000 |
| 20 | `Warlord` | Warlord of the Wasteland | 3,000 |
| 21 | `Ironclad` | Ironclad Wastelander | 3,750 |
| 22 | `Shepherd` | Shepherd of the Apocalypse | 4,500 |
| 23 | `Saint` | Post-Apocalyptic Saint | 6,000 |
| 24 | `Undying` | The Undying Ghoul Hunter | 7,500 |
| 25 | `Harbinger` | Harbinger of the Final Horde | 12,500 |
| 26 | `NukeSurvivor` | Nuclear Winter Survivor | 18,000 |
| 27 | `RadKing` | The Rad-Scorpion King | 25,000 |
| 28 | `Overlord` | Irradiated Overlord | 37,500 |
| 29 | `ChosenOne` | Chosen One of the Wasteland | 50,000 |
| 30 | `LastHope` | Last Hope of Humanity | 100,000 |

## Leaderboards

The mod automatically displays player leaderboards both on demand and at regular intervals.

### Automatic Broadcasts

- **On Login:** When a player joins, the top N players are displayed in global chat (if `ShowLeaderboardOnLogin` is enabled).
- **Periodic:** Every N in-game hours, the leaderboard is re-broadcast to all connected players (if `ShowLeaderboardIntervalHours` > 0).
- **Customizable:** Configure thresholds in `TitlesRanks.xml` — see [Configuration](#configuration) above.

### Manual Leaderboards

Use console/chat commands to display leaderboards on demand.

## Console Commands

Available in the server console, via Telnet/RCON, or **in-game chat** (prefix with `/`).
Aliases: `ranks`, `title`, `/ranks`, `/title`.

| Command | Description |
|---------|-------------|
| `/rank` | Show your current rank (in chat) |
| `/rank check [name]` | Show rank and stats — works for online **and offline** players |
| `/rank set <name> <kills>` | **Admin** — set a player's kill count (online or offline) |
| `/rank top [n]` | Show leaderboard — top 10 (or top N, max 50) online players |
| `/rank top all [n]` | Show all-time leaderboard — includes offline players |
| `/rank top <start-end>` | Show an online leaderboard range (e.g. `/rank top 4-8`) |
| `/rank top all <start-end>` | Show an all-time leaderboard range (e.g. `/rank top all 11-20`) |

**Offline Player Support:** All rank commands work with offline players by loading saved data from disk. Use player names to look up offline stats. Player data is persisted and survives server restarts.

## Building from Source

**Requirements:** [.NET SDK 8+](https://dotnet.microsoft.com/download) and a 7DTD server installation.

```bash
# Build and deploy to your server's Mods folder
./build.sh /path/to/7dtd /path/to/7dtd/Mods

# Run unit tests (no game installation required)
dotnet test TitlesSystem.Tests/
```

On Windows, use `deploy-windows.ps1` instead:

```powershell
.\deploy-windows.ps1 [-ServerRoot "C:\7dtd"] [-StartServer]
```

## Contributing

Pull requests are welcome! Please open an issue first for major changes.

## License

[MIT License](LICENSE)