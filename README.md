# 7D2D Titles System

A **server-side** mod for **7 Days to Die** that introduces a nerdy, funny rank and titles system. Players earn ranks by slaying the undead horde. Rank titles are displayed above each player's model so everyone knows who the real zombie-slaying legend is.

---

## Features

- 🧟 **19 progressive ranks** based on zombie kills — from *Freshly Irradiated Civilian* to *Chosen One of the Wasteland*
- 🪧 **Name display** — short rank title shown in brackets above each player's model (`[Warlord] PlayerName`)
- 📢 **Rank-up announcements** — global server chat notification when a player levels up
- 💾 **Persistent data** — kill counts and ranks survive server restarts, stored per world save
- 🛠️ **Admin commands** — check ranks, force-set kills, view a top-10 leaderboard
- ⚙️ **Fully configurable** — add, remove, or rename ranks in `Config/TitlesRanks.xml` without recompiling
- 🔒 **Server-side only** — no client-side installation required

---

## Ranks

| # | Short Title | Full Title | Kills Required |
|---|-------------|------------|----------------|
| 1 | `Civilian` | Freshly Irradiated Civilian | 0 |
| 2 | `Diver` | Dumpster Diver of Doom | 10 |
| 3 | `Rusty` | Rusty Nail Enthusiast | 25 |
| 4 | `CanOpener` | Can Opener Connoisseur | 50 |
| 5 | `Wanderer` | Wandering Wastelander | 100 |
| 6 | `Scavenger` | Scavenger of the Fallen | 200 |
| 7 | `Raider` | Honorary Raider | 350 |
| 8 | `VaultKicker` | Vault Door Kickboxer | 500 |
| 9 | `Whisperer` | Mutant Whisperer | 750 |
| 10 | `LastShell` | The Last Shotgun Shell | 1,000 |
| 11 | `Duke` | Duke of the Dead Lands | 1,500 |
| 12 | `Headliner` | Horde Night Headliner | 2,500 |
| 13 | `Ambassador` | Ambassador of Annihilation | 4,000 |
| 14 | `Warlord` | Warlord of the Wasteland | 6,000 |
| 15 | `Shepherd` | Shepherd of the Apocalypse | 9,000 |
| 16 | `Undying` | The Undying Ghoul Hunter | 15,000 |
| 17 | `Harbinger` | Harbinger of the Final Horde | 25,000 |
| 18 | `RadKing` | The Rad-Scorpion King | 50,000 |
| 19 | `ChosenOne` | Chosen One of the Wasteland | 100,000 |

---

## Installation

### Requirements

- 7 Days to Die **dedicated server** (version 1.x)
- No client-side installation needed

### Steps

1. **Build the mod** (see [Building from Source](#building-from-source)) or download a pre-built release.
2. Copy the following into your server's `Mods/` directory:

```
<ServerRoot>/Mods/TitlesSystem/
├── ModInfo.xml
├── TitlesSystem.dll
└── Config/
    └── TitlesRanks.xml
```

3. Restart the server. You should see in the server log:

```
[TitlesSystem] Initializing Titles System mod...
[TitlesSystem] Loaded 19 ranks from .../Config/TitlesRanks.xml
[TitlesSystem] Harmony patches applied.
[TitlesSystem] Titles System mod initialized successfully.
```

---

## Configuration

Edit `Config/TitlesRanks.xml` to customise ranks and behaviour. Changes take effect on the next server restart.

### Settings

```xml
<Settings>
    <!-- Show short rank title in brackets above player name in-game -->
    <ShowRankInName value="true"/>

    <!-- Broadcast a global chat announcement on rank-up -->
    <AnnounceRankUp value="true"/>

    <!-- Autosave interval in minutes (0 = save on disconnect only) -->
    <AutoSaveIntervalMinutes value="5"/>
</Settings>
```

### Adding or Modifying Ranks

```xml
<Ranks>
    <!-- kills: zombie kills needed | title: full announcement name | shortTitle: shown above head -->
    <Rank kills="0"   title="Your Custom Rank" shortTitle="Custom"/>
    <Rank kills="100" title="Another Rank"      shortTitle="Another"/>
</Ranks>
```

Ranks **must** be ordered by ascending `kills` value.

---

## Console Commands

These commands are available in the server console, via Telnet, and via CSMM/other RCON tools. Players with appropriate admin permissions can also use them in-game.

| Command | Description |
|---------|-------------|
| `rank` | List all rank tiers and their kill thresholds |
| `rank check` | Show your own current rank |
| `rank check <name/entityId>` | Show another player's rank |
| `rank set <name/entityId> <kills>` | **Admin** — forcibly set a player's kill count |
| `rank top` | Show top 10 online players by kill count |
| `rank top <n>` | Show top N online players (max 50) |

**Aliases:** `ranks`, `title` (all map to the same command handler)

### Examples

```
# In server console:
rank
rank check SteamUser123
rank set SteamUser123 1000
rank top 5
```

---

## Data Storage

Player rank data is stored as individual XML files in the world's save directory:

```
<SaveGameFolder>/<WorldName>/TitlesSystem/<SteamID>.xml
```

Example file content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PlayerRankData playerId="76561198012345678">
  <OriginalName>CoolPlayer</OriginalName>
  <ZombieKills>1337</ZombieKills>
  <CurrentRankIndex>9</CurrentRankIndex>
  <LastSeen>2025-06-15T18:30:00.0000000Z</LastSeen>
</PlayerRankData>
```

Data is saved:
- When a player disconnects
- Every `AutoSaveIntervalMinutes` minutes (if configured)
- When the server shuts down gracefully

---

## Building from Source

### Prerequisites

- [.NET SDK 6+](https://dotnet.microsoft.com/download) **or** MSBuild (Visual Studio 2019+)
- 7 Days to Die server installed (needed for game DLL references)

### Quick Build

```bash
# Clone the repository
git clone https://github.com/Jaydee94/7d2d-titles-system.git
cd 7d2d-titles-system

# Build (auto-detects game installation)
./build.sh

# Build and deploy directly to the Mods folder
./build.sh /path/to/7dtd /path/to/7dtd/Mods
```

### Manual Build

```bash
cd TitlesSystem
dotnet build -p:GameRoot=/path/to/7dtd -c Release
```

The compiled DLL is output to `TitlesSystem/bin/Release/TitlesSystem.dll`.

### Setting `GameRoot`

If `./build.sh` cannot auto-detect your game installation, set the `GAME_ROOT` environment variable:

```bash
export GAME_ROOT=/opt/7dtd
./build.sh
```

Or pass it directly:

```bash
./build.sh /opt/7dtd
```

---

## How It Works

### Kill Tracking

A **Harmony postfix patch** on `EntityAlive.Kill` intercepts the death of every `EntityZombie`. When the killer is identified as a player (via `ConnectionManager`), the player's kill count is incremented in `RankManager`.

### Name Display

When a player spawns or ranks up, `RankManager` sets `EntityPlayer.entityName` to `"[ShortTitle] OriginalName"` and flags `bPlayerDirty = true`. The 7DTD engine then syncs this name change to all connected clients, causing the updated rank title to appear above the player model.

### Persistence

Per-player data is serialized to XML using .NET's built-in `XmlSerializer`. Data is saved to the active world's save directory so each world maintains its own rankings.

---

## Contributing

Pull requests are welcome! Please open an issue first for major changes.

---

## License

[MIT License](LICENSE)