// GameTypes.cs — Stub type definitions for CI builds.
//
// These types mirror the public surface of the 7 Days to Die game assemblies
// (Assembly-CSharp.dll, UnityEngine.CoreModule.dll, 0Harmony.dll) so that
// TitlesSystem.csproj can compile in GitHub Actions without a game installation.
//
// Types are declared in the global namespace to match the game's assemblies.
// NOTHING here is executed at runtime — the real game DLLs take precedence.

using System;
using System.Collections.Generic;

// ---------------------------------------------------------------------------
// Mod API
// ---------------------------------------------------------------------------

public interface IModApi
{
    void InitMod(Mod modInstance);
}

public class Mod
{
    public string Path { get; set; }
}

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------

public static class Log
{
    public static void Out(string msg) { }
    public static void Warning(string msg) { }
    public static void Error(string msg) { }
}

// ---------------------------------------------------------------------------
// Events
// ---------------------------------------------------------------------------

public class ModEventSimple
{
    public void RegisterHandler(Action handler) { }
}

public class ModEventPlayerSpawned
{
    public void RegisterHandler(Action<ClientInfo, RespawnType, Vector3i> handler) { }
}

public class ModEventPlayerDisconnected
{
    public void RegisterHandler(Action<ClientInfo, bool> handler) { }
}

public static class ModEvents
{
    public static readonly ModEventSimple GameStartDone = new ModEventSimple();
    public static readonly ModEventPlayerSpawned PlayerSpawnedInWorld = new ModEventPlayerSpawned();
    public static readonly ModEventPlayerDisconnected PlayerDisconnected = new ModEventPlayerDisconnected();
}

// ---------------------------------------------------------------------------
// Client / Player identity
// ---------------------------------------------------------------------------

public class ClientInfo
{
    public string playerId;
    public string playerName;
    public int entityId;
}

public enum RespawnType
{
    NewGame,
    LoadedGame,
    Died,
    JoinMultiplayer,
    EnterMultiplayer,
}

public struct Vector3i
{
    public int x, y, z;
}

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------

public class EntityAlive
{
    public string entityName;

    public virtual void Kill(DamageResponse _dmResponse) { }
}

public class EntityZombie : EntityAlive { }

public class EntityPlayer : EntityAlive
{
    public bool bPlayerDirty;
}

public struct DamageResponse
{
    public DamageSource Source;
}

public class DamageSource
{
    public int BoundEntityId;
}

// ---------------------------------------------------------------------------
// World / Game state
// ---------------------------------------------------------------------------

public class WorldPlayerBase
{
    public Dictionary<int, EntityPlayer> dict { get; } = new Dictionary<int, EntityPlayer>();
}

public class World
{
    public WorldPlayerBase Players { get; } = new WorldPlayerBase();
}

public class AdminTools
{
    public int GetUserPermissionLevel(string playerId) => 1000;
}

public class GameManager
{
    public static GameManager Instance { get; } = new GameManager();
    public World World { get; } = new World();
    public AdminTools adminTools { get; } = new AdminTools();

    public void ChatMessageServer(
        ClientInfo _cInfo,
        EChatType _type,
        int _senderId,
        string _msg,
        string _mainName,
        bool _localizeMain,
        List<int> _recipientEntityIds) { }
}

// ---------------------------------------------------------------------------
// Connection management
// ---------------------------------------------------------------------------

public class ClientList
{
    public List<ClientInfo> list { get; } = new List<ClientInfo>();
    public ClientInfo GetForEntityId(int entityId) => null;
    public ClientInfo GetForPlayerId(string playerId) => null;
}

public class ConnectionManager
{
    public static ConnectionManager Instance { get; } = new ConnectionManager();
    public ClientList Clients { get; } = new ClientList();
}

// ---------------------------------------------------------------------------
// Game preferences
// ---------------------------------------------------------------------------

public static class GamePrefs
{
    public static string GetString(EnumGamePrefs pref) => string.Empty;
}

public enum EnumGamePrefs
{
    SaveGameFolder,
    GameName,
}

// ---------------------------------------------------------------------------
// Chat
// ---------------------------------------------------------------------------

public enum EChatType
{
    Global,
    Friends,
    Team,
    Whisper,
}

// ---------------------------------------------------------------------------
// Console / command system
// ---------------------------------------------------------------------------

public abstract class ConsoleCmdAbstract
{
    protected abstract string[] getCommands();
    protected abstract string getDescription();
    protected virtual string getHelp() => string.Empty;
    public abstract void Execute(List<string> _params, CommandSenderInfo _senderInfo);
}

public struct CommandSenderInfo
{
    public ClientInfo RemoteClientInfo;
}

public class SdtdConsole
{
    public static SdtdConsole Instance { get; } = new SdtdConsole();
    public void Output(string msg) { }
}
