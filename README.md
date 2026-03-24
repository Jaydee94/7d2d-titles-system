# 7D2D Titles System

A **server-side** mod for **7 Days to Die** (v2.5) that introduces a funny, apocalyptic rank and titles system. Players earn ranks by slaying the undead horde. Rank titles are displayed above each player's model so everyone knows who the real wasteland legend is.

---

## Features

- 🧟 **30 progressive ranks** based on zombie kills — from *Freshly Irradiated Civilian* to *Last Hope of Humanity*
- 🪧 **Name display** — short rank title shown in brackets above each player's model (`[Warlord] PlayerName`)
- 📢 **Rank-up announcements** — global server chat notification when a player levels up
- 💾 **Persistent data** — kill counts and ranks survive server restarts, stored per world save
- 🛠️ **Admin commands** — check ranks, force-set kills, view a top-10 leaderboard
- ⚙️ **Fully configurable** — add, remove, or rename ranks in `Config/TitlesRanks.xml` without recompiling
- 🔒 **Server-side only** — no client-side installation required
- 🚀 **Automated releases** — compiled mod is published as a GitHub Release on every merge to `main`

---

## Ranks

| # | Short Title | Full Title | Kills Required |
|---|-------------|------------|----------------|
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

---

## Installation

### Requirements

- 7 Days to Die **dedicated server v2.5** (or newer compatible release)
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
[TitlesSystem] Loaded 30 ranks from .../Config/TitlesRanks.xml
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
| `rank` or `/rank` | In-game chat: show your current rank. Server console/Telnet: list all rank tiers |
| `rank check` | Show your own current rank |
| `rank check <name/entityId>` | Show another player's rank |
| `rank set <name/entityId> <kills>` | **Admin** — forcibly set a player's kill count |
| `rank top` | Show top 10 online players by kill count |
| `rank top <n>` | Show top N online players (max 50) |

**Aliases:** `ranks`, `title`, `/ranks`, `/title` (all map to the same command handler)

### Command Output Location

- If a player runs `rank` commands in-game, the response is sent to that player's chat.
- The same response is also written to the server console.
- If the command is run from server console / Telnet / RCON, output appears in that console channel as usual.
- Bare `rank` from in-game behaves like `rank check` (self rank).
- Compatibility note: on server versions that do not auto-route chat text to console commands, the mod intercepts chat input directly so `rank` and `/rank` still work from global chat.

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

- [.NET SDK 8+](https://dotnet.microsoft.com/download)
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

### Running Unit Tests

The core rank calculation and data-persistence logic is covered by an xUnit test suite that runs without any game installation. Tests run on .NET 8 and require no game DLLs:

```bash
dotnet test TitlesSystem.Tests/
```

All 35 tests should pass and cover:
- Rank boundary computation (`RankCalculator`)
- `PlayerRankData` XML serialization round-trips
- `RankDefinition` construction and formatting

The same `dotnet test` step runs automatically on every pull request via the CI pipeline.

---

## Testing Locally (Linux)

These steps walk through a full local test cycle using a 7DTD dedicated server running on the same machine as your development environment.

### 1. Install the 7DTD Dedicated Server

Use SteamCMD to install the **7 Days to Die Dedicated Server** (App ID 294420).

```bash
# Anonymous SteamCMD download
steamcmd +force_install_dir /opt/7dtd \
         +login anonymous \
         +app_update 294420 validate \
         +quit
```

### 2. Build and Deploy the Mod

```bash
# Build and copy directly to the server's Mods/ folder in one step
./build.sh /opt/7dtd /opt/7dtd/Mods
```

This places the following files under `/opt/7dtd/Mods/TitlesSystem/`:

```
TitlesSystem/
├── ModInfo.xml
├── TitlesSystem.dll
└── Config/
    └── TitlesRanks.xml
```

### 3. Start the Server and Verify the Mod Loads

```bash
cd /opt/7dtd
./startserver.sh -configfile=serverconfig.xml
```

Watch the console output for:

```
[TitlesSystem] Initializing Titles System mod...
[TitlesSystem] Loaded 30 ranks from .../Config/TitlesRanks.xml
[TitlesSystem] Harmony patches applied.
[TitlesSystem] Titles System mod initialized successfully.
```

### 4. Test with Console Commands

Connect to the server console (telnet, Alloc's web-panel, etc.) and run:

```
rank
rank check YourSteamName
rank set YourSteamName 1000
rank top 10
```

### 5. Iterate Without Restarting the Server

> The server **must be restarted** to pick up a rebuilt DLL.
> However, you can edit `Config/TitlesRanks.xml` rank thresholds and reload
> them by restarting only — no recompile needed.

Typical dev loop:

```bash
# 1. Edit source files
# 2. Rebuild and redeploy
./build.sh /opt/7dtd /opt/7dtd/Mods
# 3. Restart the dedicated server
# 4. Connect and test with 'rank set' to jump to any kill count
```

### 6. Confirm Rank Display In-Game

After `rank set <player> <kills>`:
- The player's name above their character model updates to `[ShortTitle] PlayerName`.
- A global chat announcement is broadcast (if `AnnounceRankUp` is `true`).
- Disconnect and reconnect to verify data persists across sessions.

---

## Testing Locally (Windows 11)

These steps walk through the same test cycle on a Windows 11 machine. A helper script (`deploy-windows.ps1`) handles the build and deploy steps for you.

### Prerequisites

- [.NET SDK 8+](https://dotnet.microsoft.com/download) installed
- [Steam](https://store.steampowered.com/about/) installed (for the dedicated server tool)
- The repository cloned locally (e.g. to `C:\dev\7d2d-titles-system`)

### 1. Install the 7DTD Dedicated Server

1. Open **Steam → Library → Tools** and search for **7 Days to Die Dedicated Server**.
2. Install it. Steam places the server under a path like:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\
   ```

> **Tip:** You can also use [SteamCMD for Windows](https://developer.valvesoftware.com/wiki/SteamCMD) for a headless install:
> ```powershell
> steamcmd +force_install_dir "C:\7dtd" `
>          +login anonymous `
>          +app_update 294420 validate `
>          +quit
> ```

### 2. Build and Deploy with `deploy-windows.ps1`

Open **PowerShell** in the repository root and run:

```powershell
# Auto-detects the server under the default Steam path
.\deploy-windows.ps1

# Or provide an explicit server path
.\deploy-windows.ps1 -ServerRoot "C:\7dtd"

# Build, deploy, and start the server in one step
.\deploy-windows.ps1 -StartServer
```

From **WSL**, use the Windows PowerShell host and pass script arguments after the converted script path:

```bash
# Build and deploy from WSL
powershell.exe -ExecutionPolicy Bypass -File "$(wslpath -w ./deploy-windows.ps1)"

# Build, deploy, and start the server from WSL
powershell.exe -ExecutionPolicy Bypass -File "$(wslpath -w ./deploy-windows.ps1)" -StartServer
```

The script:
- Auto-detects your server installation from common Steam paths
- Builds the mod with game DLLs (or falls back to CI stubs if DLLs aren't found)
- Copies the DLL, `ModInfo.xml`, and `TitlesRanks.xml` into `<ServerRoot>\Mods\TitlesSystem\`
- Optionally launches the dedicated server (`-StartServer`)

> **First run only:** PowerShell may block scripts by default. Run this once to allow local scripts:
> ```powershell
> Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
> ```

The resulting mod layout:

```
<ServerRoot>\Mods\TitlesSystem\
├── ModInfo.xml
├── TitlesSystem.dll
└── Config\
    └── TitlesRanks.xml
```

### 3. Start the Server and Verify the Mod Loads

If you did not pass `-StartServer` above:

```powershell
cd "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
.\startdedicated.bat
```

Some installs may use `StartDedicatedServer.bat` instead. The deploy script now detects `StartDedicatedServer.bat`, `startdedicated.bat`, or `7DaysToDieServer.exe` automatically when `-StartServer` is used.

Watch the console window for:

```
[TitlesSystem] Initializing Titles System mod...
[TitlesSystem] Loaded 30 ranks from ...\Config\TitlesRanks.xml
[TitlesSystem] Harmony patches applied.
[TitlesSystem] Titles System mod initialized successfully.
```

### 4. Test with Console Commands

The server opens a console window on Windows. You can also connect via Telnet if enabled in `serverconfig.xml`. Run:

```
rank
rank check YourSteamName
rank set YourSteamName 1000
rank top 10
```

If you run these commands from an in-game player, results are shown in that player's chat.

### 5. Iterate Without Restarting the Server

> The server **must be restarted** to pick up a rebuilt DLL. You can edit
> `Config\TitlesRanks.xml` without recompiling — only a server restart is needed
> to reload rank definitions.

Typical dev loop on Windows:

```powershell
# 1. Edit source files in your editor
# 2. Stop the dedicated server (close the window or Ctrl+C)
# 3. Rebuild and redeploy in one command
.\deploy-windows.ps1
# 4. Start the server again and test with 'rank set'
```

Or combine deploy and start:

```powershell
.\deploy-windows.ps1 -StartServer
```

### 6. Confirm Rank Display In-Game

After `rank set <player> <kills>`:
- The player's name above their character model updates to `[ShortTitle] PlayerName`.
- A global chat announcement is broadcast (if `AnnounceRankUp` is `true`).
- Disconnect and reconnect to verify data persists across sessions.

### 7. Runtime Smoke Test (Windows + WSL)

Use this quick sequence after each code change:

1. Deploy from WSL:

    powershell.exe -ExecutionPolicy Bypass -File "$(wslpath -w ./deploy-windows.ps1)"

2. Or deploy and start in one command from WSL:

    powershell.exe -ExecutionPolicy Bypass -File "$(wslpath -w ./deploy-windows.ps1)" -StartServer

3. If starting manually on Windows:

    C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\startdedicated.bat

4. In the server console, run:

    rank
    rank check YourSteamName
    rank set YourSteamName 1000
    rank top 10

5. Verify log output contains TitlesSystem initialization lines.

    Typical log folder on this setup:
    C:\Users\<YourUser>\AppData\Roaming\7DaysToDie\logs

6. Stop and restart the server, then run `rank check` again to confirm persisted data.

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