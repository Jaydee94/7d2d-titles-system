# 7D2D Titles System

A **server-side** mod for **7 Days to Die** (v2.5) that gives players apocalyptic rank titles based on zombie kills. Titles are displayed above each player's model and announced in chat on rank-up.

## Features

- 🧟 **30 progressive ranks** based on zombie kills — from *Freshly Irradiated Civilian* to *Last Hope of Humanity*
- 🪧 **Name display** — short rank title shown in brackets above each player's model (`[Warlord] PlayerName`)
- 📢 **Rank-up announcements** — global server chat notification when a player levels up
- 💾 **Persistent data** — kill counts and ranks survive server restarts, stored per world save
- 🛠️ **Admin commands** — check ranks, force-set kills, view a leaderboard
- ⚙️ **Fully configurable** — add, remove, or rename ranks in `Config/TitlesRanks.xml` without recompiling
- 🔒 **Server-side only** — no client-side installation required

## Installation

1. Download the latest release or [build from source](#building-from-source).
2. Copy `TitlesSystem/` (containing `ModInfo.xml`, `TitlesSystem.dll`, and `Config/TitlesRanks.xml`) into your server's `Mods/` directory.
3. Restart the server.

## Configuration

Edit `Config/TitlesRanks.xml` to customise settings and ranks. Changes take effect on the next server restart.

```xml
<Settings>
    <ShowRankInName value="true"/>          <!-- show rank above player head -->
    <AnnounceRankUp value="true"/>          <!-- broadcast rank-up in chat -->
    <AutoSaveIntervalMinutes value="5"/>    <!-- 0 = save on disconnect only -->
</Settings>

<Ranks>
    <!-- ranks must be ordered by ascending kills value -->
    <Rank kills="0"   title="Your Custom Rank" shortTitle="Custom"/>
    <Rank kills="100" title="Another Rank"      shortTitle="Another"/>
</Ranks>
```

## Ranks

| # | Short Title | Full Title | Kills |
|---|-------------|------------|-------|
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
Aliases: `ranks`, `title`, `/ranks`, `/title`.

| Command | Description |
|---------|-------------|
| `rank` | Show your current rank (in-game) or list all rank tiers (console) |
| `rank check [name/entityId]` | Show your own or another player's rank |
| `rank set <name/entityId> <kills>` | **Admin** — set a player's kill count |
| `rank top [n]` | Show top 10 (or top N, max 50) players by kills |

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