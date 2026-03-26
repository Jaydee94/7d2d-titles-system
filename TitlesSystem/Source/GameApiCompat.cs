using System;
using System.Collections;
using System.Reflection;

public static class Log
{
    public static void Out(string message) => Write(message);
    public static void Warning(string message) => Write("[WARN] " + message);
    public static void Error(string message) => Write("[ERROR] " + message);

    private static void Write(string message)
    {
        try
        {
            SdtdConsole.Instance?.Output(message);
        }
        catch
        {
            // Best effort only.
        }
    }
}

namespace TitlesSystem
{
    internal static class GameApiCompat
    {
        public static string GetPlayerId(ClientInfo clientInfo)
        {
            if (clientInfo == null) return null;

            // Old API: public string playerId field
            var field = clientInfo.GetType().GetField("playerId", BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var id = field.GetValue(clientInfo) as string;
                if (!string.IsNullOrEmpty(id)) return id;
            }

            // New API: InternalId / PlatformId / CrossplatformId (PlatformUserIdentifierAbs)
            var candidates = new[] { "InternalId", "PlatformId", "CrossplatformId" };
            foreach (var name in candidates)
            {
                object value = GetMemberValue(clientInfo, name);
                if (value == null) continue;

                var combined = value.GetType().GetProperty("CombinedString", BindingFlags.Public | BindingFlags.Instance);
                if (combined != null)
                {
                    var id = combined.GetValue(value, null) as string;
                    if (!string.IsNullOrEmpty(id)) return id;
                }

                var asString = value.ToString();
                if (!string.IsNullOrEmpty(asString)) return asString;
            }

            return null;
        }

        public static ClientInfo GetClientInfoByEntityId(int entityId)
        {
            if (entityId < 0) return null;
            object clients = ConnectionManager.Instance?.Clients;
            if (clients == null) return null;

            object found = InvokeMember(clients, "GetForEntityId", entityId)
                ?? InvokeMember(clients, "ForEntityId", entityId);

            if (found is ClientInfo info) return info;

            var list = GetMemberValue(clients, "list") as IEnumerable;
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (!(item is ClientInfo client)) continue;
                    object value = GetMemberValue(client, "entityId");
                    if (value is int id && id == entityId) return client;
                }
            }

            return null;
        }

        public static ClientInfo GetClientInfoByPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            object clients = ConnectionManager.Instance?.Clients;
            if (clients == null) return null;

            object byOldApi = InvokeMember(clients, "GetForPlayerId", playerId);
            if (byOldApi is ClientInfo oldInfo) return oldInfo;

            // New API path: parse combined ID and query ForUserId(PlatformUserIdentifierAbs)
            try
            {
                Type idType = Type.GetType("PlatformUserIdentifierAbs");
                if (idType != null)
                {
                    MethodInfo fromCombined = idType.GetMethod("FromCombinedString", BindingFlags.Public | BindingFlags.Static);
                    if (fromCombined != null)
                    {
                        object userId = fromCombined.Invoke(null, new object[] { playerId, false });
                        if (userId != null)
                        {
                            object byUserId = InvokeMember(clients, "ForUserId", userId);
                            if (byUserId is ClientInfo info) return info;
                        }
                    }
                }
            }
            catch
            {
                // Fallback below.
            }

            var list = GetMemberValue(clients, "list") as IEnumerable;
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item is ClientInfo client && string.Equals(GetPlayerId(client), playerId, StringComparison.OrdinalIgnoreCase))
                        return client;
                }
            }

            return null;
        }

        public static int GetEntityId(ClientInfo clientInfo)
        {
            if (clientInfo == null) return -1;
            object value = GetMemberValue(clientInfo, "entityId");
            return value is int id ? id : -1;
        }

        public static int GetKillerEntityId(DamageSource source)
        {
            if (source == null) return -1;

            object boundEntity = GetMemberValue(source, "BoundEntityId")
                ?? GetMemberValue(source, "ownerEntityId")
                ?? GetMemberValue(source, "CreatorEntityId");

            if (boundEntity is int idFromField) return idFromField;

            object idFromMethod = InvokeMember(source, "getEntityId");
            if (idFromMethod is int idFromMethodInt) return idFromMethodInt;

            return -1;
        }

        public static bool IsAdmin(CommandSenderInfo sender)
        {
            ClientInfo remote = sender.RemoteClientInfo;
            if (remote == null) return true;

            object adminTools = GameManager.Instance?.adminTools;
            if (adminTools == null) return false;

            string playerId = GetPlayerId(remote);

            // Old API
            object level = InvokeMember(adminTools, "GetUserPermissionLevel", playerId);
            if (level is int permissionLevel) return permissionLevel < 1000;

            // New API
            object allowed = InvokeMember(adminTools, "CommandAllowedFor", new[] { "rank" }, remote);
            if (allowed is bool isAllowed) return isAllowed;

            return false;
        }

        public static string GetSaveGameDir()
        {
            try
            {
                object fromGameIo = InvokeStatic(typeof(GameIO), "GetSaveGameDir");
                if (fromGameIo is string saveDir && !string.IsNullOrEmpty(saveDir)) return saveDir;
            }
            catch
            {
                // Try legacy path below.
            }

            try
            {
                // Legacy path lookup without hard-referencing enum members that may no longer exist.
                Type enumType = typeof(EnumGamePrefs);
                object saveFolderEnum = Enum.Parse(enumType, "SaveGameFolder", ignoreCase: true);

                MethodInfo getString = typeof(GamePrefs).GetMethod("GetString", BindingFlags.Public | BindingFlags.Static);
                if (getString != null)
                {
                    object value = getString.Invoke(null, new[] { saveFolderEnum });
                    if (value is string legacyDir && !string.IsNullOrEmpty(legacyDir)) return legacyDir;
                }
            }
            catch
            {
                // No compatible save dir API found.
            }

            return null;
        }

        public static void ChatMessageGlobal(string message)
        {
            object gm = GameManager.Instance;
            if (gm == null || string.IsNullOrEmpty(message)) return;

            // Support both old and new ChatMessageServer signatures.
            MethodInfo[] methods = gm.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (!string.Equals(method.Name, "ChatMessageServer", StringComparison.Ordinal)) continue;

                ParameterInfo[] p = method.GetParameters();
                if (p.Length != 7) continue;

                try
                {
                    object[] args;
                    if (p[4].ParameterType == typeof(string))
                    {
                        // Old signature:
                        // (ClientInfo, EChatType, int, string, string, bool, List<int>)
                        args = new object[] { null, EChatType.Global, -1, message, "TitlesSystem", false, null };
                    }
                    else
                    {
                        // New signature:
                        // (ClientInfo, EChatType, int, string, List<int>, EMessageSender, BbCodeSupportMode)
                        object senderMode = p[5].ParameterType.IsValueType
                            ? Activator.CreateInstance(p[5].ParameterType)
                            : null;
                        object bbMode = p[6].ParameterType.IsValueType
                            ? Activator.CreateInstance(p[6].ParameterType)
                            : null;
                        args = new object[] { null, EChatType.Global, -1, message, null, senderMode, bbMode };
                    }

                    method.Invoke(gm, args);
                    return;
                }
                catch
                {
                    // Try next overload if this one fails.
                }
            }
        }

        public static void ChatMessageToClient(ClientInfo clientInfo, string message)
        {
            if (clientInfo == null || string.IsNullOrEmpty(message)) return;

            int entityId = GetEntityId(clientInfo);
            if (entityId < 0) return;

            object gm = GameManager.Instance;
            if (gm == null) return;

            MethodInfo[] methods = gm.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (!string.Equals(method.Name, "ChatMessageServer", StringComparison.Ordinal)) continue;

                ParameterInfo[] p = method.GetParameters();
                if (p.Length != 7) continue;

                try
                {
                    var recipients = new System.Collections.Generic.List<int> { entityId };
                    object[] args;
                    if (p[4].ParameterType == typeof(string))
                    {
                        // Old signature:
                        // (ClientInfo, EChatType, int, string, string, bool, List<int>)
                        args = new object[] { null, EChatType.Whisper, entityId, message, "TitlesSystem", false, recipients };
                    }
                    else
                    {
                        // New signature:
                        // (ClientInfo, EChatType, int, string, List<int>, EMessageSender, BbCodeSupportMode)
                        object senderMode = p[5].ParameterType.IsValueType
                            ? Activator.CreateInstance(p[5].ParameterType)
                            : null;
                        object bbMode = p[6].ParameterType.IsValueType
                            ? Activator.CreateInstance(p[6].ParameterType)
                            : null;
                        args = new object[] { null, EChatType.Whisper, entityId, message, recipients, senderMode, bbMode };
                    }

                    method.Invoke(gm, args);
                    return;
                }
                catch
                {
                    // Try alternative argument combinations before moving on.
                    try
                    {
                        var recipients = new System.Collections.Generic.List<int> { entityId };
                        object[] fallbackArgs;
                        if (p[4].ParameterType == typeof(string))
                        {
                            // Old signature fallback to Global and senderless server output.
                            fallbackArgs = new object[] { null, EChatType.Global, -1, message, "TitlesSystem", false, recipients };
                        }
                        else
                        {
                            // New signature fallback to Global and senderless server output.
                            object senderMode = p[5].ParameterType.IsValueType
                                ? Activator.CreateInstance(p[5].ParameterType)
                                : null;
                            object bbMode = p[6].ParameterType.IsValueType
                                ? Activator.CreateInstance(p[6].ParameterType)
                                : null;
                            fallbackArgs = new object[] { null, EChatType.Global, -1, message, recipients, senderMode, bbMode };
                        }

                        method.Invoke(gm, fallbackArgs);
                        return;
                    }
                    catch
                    {
                        // Try next overload if this one fails.
                    }
                }
            }
        }

        /// <summary>
        /// Marks the player entity dirty so the server sends its updated stats (including
        /// entityName) to all clients, and also attempts to broadcast a
        /// NetPackageEntityNameChange packet directly.  The direct broadcast is necessary
        /// because in 7DTD v2.x the NetPackagePlayerStats packet that is triggered by the
        /// dirty flags may not include entityName.
        /// </summary>
        public static void MarkPlayerNameDirty(EntityPlayer player, int entityId = -1)
        {
            if (player == null) return;

            // Primary: set dirty flags so the server re-broadcasts player stats on the next tick.
            TrySetMemberValue(player, "bPlayerDirty", true);
            TrySetMemberValue(player, "bPlayerStatsChanged", true);

            // Secondary: explicitly send a NetPackageEntityNameChange to all connected clients.
            // This is more reliable across game versions for syncing entityName changes.
            if (entityId < 0)
            {
                object idObj = GetMemberValue(player, "entityId");
                if (idObj is int id) entityId = id;
            }

            if (entityId >= 0)
                TrySendEntityNameChange(entityId, player.entityName);
        }

        // Cached type references — resolved once on first use and reused for all
        // subsequent calls, so the AppDomain assembly scan is only paid once per
        // server lifetime.
        private static Type _netPkgManagerType;
        private static Type _netPkgEntityNameChangeType;
        private static bool _netPkgTypesResolved;

        private static void ResolveNetPackageTypes()
        {
            if (_netPkgTypesResolved) return;
            _netPkgTypesResolved = true;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (_netPkgManagerType == null)
                        _netPkgManagerType = asm.GetType("NetPackageManager");
                    if (_netPkgEntityNameChangeType == null)
                        _netPkgEntityNameChangeType = asm.GetType("NetPackageEntityNameChange");
                }
                catch { /* skip assemblies that can't be reflected */ }

                if (_netPkgManagerType != null && _netPkgEntityNameChangeType != null)
                    break;
            }

            if (_netPkgManagerType == null)
                Log.Warning("[TitlesSystem] NetPackageManager not found — entity-name packets will rely on dirty flags only.");
            if (_netPkgEntityNameChangeType == null)
                Log.Warning("[TitlesSystem] NetPackageEntityNameChange not found — entity-name packets will rely on dirty flags only.");
        }

        /// <summary>
        /// Sends a NetPackageEntityNameChange for <paramref name="entityId"/> to every
        /// connected client via ConnectionManager, using reflection so the code compiles
        /// without direct references to the game DLLs.
        /// </summary>
        private static void TrySendEntityNameChange(int entityId, string newName)
        {
            if (entityId < 0 || string.IsNullOrEmpty(newName)) return;

            try
            {
                ResolveNetPackageTypes();
                if (_netPkgManagerType == null || _netPkgEntityNameChangeType == null) return;

                // pkg = NetPackageManager.GetPackage<NetPackageEntityNameChange>()
                MethodInfo getPackage = _netPkgManagerType.GetMethod(
                    "GetPackage", BindingFlags.Public | BindingFlags.Static);
                if (getPackage == null) return;

                object pkg = getPackage
                    .MakeGenericMethod(_netPkgEntityNameChangeType)
                    .Invoke(null, null);
                if (pkg == null) return;

                // pkg.Setup(entityId, newName)  — parameter count may vary by version
                MethodInfo setup = _netPkgEntityNameChangeType.GetMethod(
                    "Setup", BindingFlags.Public | BindingFlags.Instance);
                if (setup != null)
                {
                    ParameterInfo[] ps = setup.GetParameters();
                    if (ps.Length >= 2)
                        setup.Invoke(pkg, new object[] { entityId, newName });
                    else if (ps.Length == 1)
                        setup.Invoke(pkg, new object[] { entityId });
                }

                // ConnectionManager.Instance.SendPackage(pkg [, true])
                object cm = ConnectionManager.Instance;
                if (cm == null) return;

                MethodInfo send = cm.GetType().GetMethod(
                    "SendPackage", BindingFlags.Public | BindingFlags.Instance);
                if (send == null) return;

                ParameterInfo[] sendPs = send.GetParameters();
                if (sendPs.Length == 1)
                    send.Invoke(cm, new object[] { pkg });
                else if (sendPs.Length >= 2)
                    send.Invoke(cm, new object[] { pkg, true });
            }
            catch (Exception ex)
            {
                Log.Warning($"[TitlesSystem] TrySendEntityNameChange failed: {ex.Message}");
            }
        }

        private static object InvokeMember(object target, string methodName, params object[] args)
        {
            if (target == null) return null;
            try
            {
                return target.GetType().InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                    binder: null,
                    target: target,
                    args: args);
            }
            catch
            {
                return null;
            }
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            if (type == null) return null;
            try
            {
                return type.InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    target: null,
                    args: args);
            }
            catch
            {
                return null;
            }
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return null;

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(target);

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(target, null);

            return null;
        }

        private static void TrySetMemberValue(object target, string memberName, object value)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return;

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value, null);
            }
        }
    }
}