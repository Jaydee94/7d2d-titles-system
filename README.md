# 7D2D Titles System

7D2D Titles System is a server-side mod for 7 Days to Die that gives players apocalyptic rank titles based on zombie kills. Titles are displayed above each player's model and announced in chat on rank-up.

## What It Does

- Assigns players one of 30 progressive ranks based on total zombie kills
- Displays a short rank title in brackets above each player's model (`[Warlord] PlayerName`)
- Announces rank-up events in global server chat
- Persists kill counts and rank data across server restarts, stored per world save
- Tracks player statistics — deaths, kill streaks, playtime, K/D ratio, kills/hour, and weapon usage
- Displays a configurable leaderboard on player login and at a periodic interval
- Works on the server only — players do not need to install the mod locally

## Installation

1. Download the latest release.
2. Extract it.
3. Copy the `TitlesSystem` folder into your server's `Mods` folder.
4. Restart the server.

## Configuration

Edit `Config/TitlesRanks.xml` to customise settings and ranks. Changes take effect on the next server restart.

```xml
<Settings>
    <ShowRankInName value="true"/>
    <AnnounceRankUp value="true"/>
    <AutoSaveIntervalMinutes value="5"/>
    <ShowLeaderboardOnLogin value="true"/>
    <ShowLeaderboardIntervalHours value="6"/>
    <LeaderboardTopPlayers value="10"/>
    <AnnouncementColor value="FFD700"/>
    <LeaderboardColor value="00BFFF"/>
</Settings>
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ShowRankInName` | `true` | Display the short rank title in brackets above the player's name in-game |
| `AnnounceRankUp` | `true` | Broadcast a global server announcement when a player ranks up |
| `AutoSaveIntervalMinutes` | `5` | Save player rank data to disk every N minutes (`0` = only on disconnect/event) |
| `ShowLeaderboardOnLogin` | `true` | Show leaderboard in global chat when a player logs in |
| `ShowLeaderboardIntervalHours` | `6` | Show leaderboard in global chat every N hours (`0` = disabled) |
| `LeaderboardTopPlayers` | `10` | Number of top players to display in the leaderboard (max 50) |
| `AnnouncementColor` | `FFD700` | Chat color for rank-up announcements, as a 6-digit RRGGBB hex string. Set to empty to disable coloring. |
| `LeaderboardColor` | `00BFFF` | Chat color for leaderboard messages, as a 6-digit RRGGBB hex string. Set to empty to disable coloring. |

To add, remove, or rename ranks, edit the `<Ranks>` section. Ranks must be ordered by ascending kills:

```xml
<Ranks>
    <Rank kills="0"   title="Your Custom Rank" shortTitle="Custom"/>
    <Rank kills="100" title="Another Rank"      shortTitle="Another"/>
</Ranks>
```

## Ranks

| # | Short Title | Title | Kills Required |
|---|-------------|-------|----------------|
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

## Console Commands

Available in the server console, via Telnet/RCON, or in-game chat (prefix with `/`).
Aliases: `ranks`, `title`.

| Command | Description |
|---------|-------------|
| `/rank` | Show your current rank |
| `/rank check [name]` | Show rank and stats for an online or offline player |
| `/rank set <name> <kills>` | Admin — set a player's kill count |
| `/rank top [n]` | Show leaderboard of top online players |
| `/rank top all [n]` | Show all-time leaderboard including offline players |
| `/rank top <start-end>` | Show a leaderboard range (e.g. `/rank top 4-8`) |
| `/rank top all <start-end>` | Show an all-time leaderboard range (e.g. `/rank top all 11-20`) |

## For Development

If you want to build and deploy the mod locally, use the included scripts.

On Linux:

```bash
./build.sh /path/to/7dtd /path/to/7dtd/Mods
```

On Windows:

```powershell
.\deploy-windows.ps1 [-ServerRoot "C:\7dtd"] [-StartServer]
```

To run the unit tests (no game installation required):

```bash
dotnet test TitlesSystem.Tests/
```

## Releases and CI

This repository includes GitHub Actions for:

- building the mod on pull requests
- creating a release package on pushes to `main`

If you only want the mod, you can ignore the build setup and just download a release zip.

## Files Included in the Release

```text
TitlesSystem/
├── ModInfo.xml
├── TitlesSystem.dll
└── Config/
    └── TitlesRanks.xml
```

## License

[MIT](LICENSE)
