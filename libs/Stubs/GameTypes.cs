// GameTypes.cs - Stub type definitions for CI builds.
//
// These types mirror the public surface of key 7DTD APIs closely enough for
// compilation in CI without an installed dedicated server.

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
// Identifiers / client identity
// ---------------------------------------------------------------------------

public class PlatformUserIdentifierAbs
{
    public string CombinedString { get; set; } = string.Empty;

    public static PlatformUserIdentifierAbs FromCombinedString(string combined, bool logErrors)
    {
        return new PlatformUserIdentifierAbs { CombinedString = combined ?? string.Empty };
    }

    public override string ToString() => CombinedString;
}

public class ClientInfo
{
    public string playerId;
    public string playerName;
    public int entityId;

    public PlatformUserIdentifierAbs PlatformId { get; set; } = new PlatformUserIdentifierAbs();
    public PlatformUserIdentifierAbs CrossplatformId { get; set; } = new PlatformUserIdentifierAbs();
    public PlatformUserIdentifierAbs InternalId => PlatformId;
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

public class SdtdConsole
{
    public static SdtdConsole Instance { get; } = new SdtdConsole();
    public void Output(string msg) { }
}

// ---------------------------------------------------------------------------
// Events
// ---------------------------------------------------------------------------

public static class ModEvents
{
    public struct SGameStartDoneData { }

    public struct SPlayerSpawnedInWorldData
    {
        public ClientInfo ClientInfo;
        public bool IsLocalPlayer;
        public int EntityId;
        public RespawnType RespawnType;
        public Vector3i Position;
    }

    public struct SPlayerDisconnectedData
    {
        public ClientInfo ClientInfo;
        public bool GameShuttingDown;
    }

    public delegate void ModEventHandlerDelegate<T>(ref T data);

    public class ModEvent<T>
    {
        public void RegisterHandler(ModEventHandlerDelegate<T> handler) { }
    }

    public static readonly ModEvent<SGameStartDoneData> GameStartDone = new ModEvent<SGameStartDoneData>();
    public static readonly ModEvent<SPlayerSpawnedInWorldData> PlayerSpawnedInWorld = new ModEvent<SPlayerSpawnedInWorldData>();
    public static readonly ModEvent<SPlayerDisconnectedData> PlayerDisconnected = new ModEvent<SPlayerDisconnectedData>();
}

// ---------------------------------------------------------------------------
// Entities / damage
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
    public bool bPlayerStatsChanged;
}

public struct DamageResponse
{
    public DamageSource Source;
}

public class DamageSource
{
    public int BoundEntityId;
    public int ownerEntityId;
    public int CreatorEntityId;

    public int getEntityId() => BoundEntityId;
}

// ---------------------------------------------------------------------------
// World / game state
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
    public bool CommandAllowedFor(string[] cmdNames, ClientInfo clientInfo) => false;
}

public enum EChatType
{
    Global,
    Friends,
    Team,
    Whisper,
}

public enum EMessageSender
{
    None,
}

public static class GeneratedTextManager
{
    public enum BbCodeSupportMode
    {
        None,
    }
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

    public void ChatMessageServer(
        ClientInfo _cInfo,
        EChatType _type,
        int _senderId,
        string _msg,
        List<int> _recipientEntityIds,
        EMessageSender _msgSender,
        GeneratedTextManager.BbCodeSupportMode _bbMode) { }
}

// ---------------------------------------------------------------------------
// Connection management
// ---------------------------------------------------------------------------

public class ClientList
{
    public List<ClientInfo> list { get; } = new List<ClientInfo>();
    public ClientInfo GetForEntityId(int entityId) => null;
    public ClientInfo GetForPlayerId(string playerId) => null;
    public ClientInfo ForEntityId(int entityId) => GetForEntityId(entityId);
    public ClientInfo ForUserId(PlatformUserIdentifierAbs userIdentifier) => null;
}

public class ConnectionManager
{
    public static ConnectionManager Instance { get; } = new ConnectionManager();
    public ClientList Clients { get; } = new ClientList();
}

// ---------------------------------------------------------------------------
// Game preferences / IO
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

public static class GameIO
{
    public static string GetSaveGameDir() => string.Empty;
}

// ---------------------------------------------------------------------------
// Console / command system
// ---------------------------------------------------------------------------

public abstract class ConsoleCmdAbstract
{
    public abstract string[] getCommands();
    public abstract string getDescription();
    public virtual string getHelp() => string.Empty;
    public abstract void Execute(List<string> _params, CommandSenderInfo _senderInfo);
}

public struct CommandSenderInfo
{
    public ClientInfo RemoteClientInfo;
}
