using HarmonyLib;
using System;
using System.Reflection;

namespace TitlesSystem.Patches
{
    /// <summary>
    /// Intercepts player name retrieval methods on the server to prepend the
    /// rank title before the player name. This ensures the title appears above
    /// the player model in the 3D overhead display as well as any other location
    /// that reads the player's name via these methods.
    ///
    /// Registered manually via ApplyPatch (not via PatchAll) to be resilient
    /// against method signature changes across 7D2D versions.
    /// </summary>
    public static class PlayerNamePatch
    {
        // Method names that 7D2D uses (across different versions) to retrieve
        // the display name shown above a player's head.
        private static readonly string[] CandidateMethods =
        {
            "GetPlayerName",
            "GetDisplayName",
            "GetPrimaryPlayerName",
        };

        /// <summary>
        /// Called from TitlesSystemMod.InitMod to discover and patch every
        /// player name method that exists in this server build.
        /// </summary>
        public static void ApplyPatch(Harmony harmony)
        {
            var postfixMethod = new HarmonyMethod(typeof(PlayerNamePatch), nameof(Postfix));
            int count = 0;

            foreach (var methodName in CandidateMethods)
            {
                // Check EntityPlayer first, then fall back to EntityAlive.
                foreach (var targetType in new[] { typeof(EntityPlayer), typeof(EntityAlive) })
                {
                    MethodInfo method = targetType.GetMethod(
                        methodName,
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        Type.EmptyTypes,
                        null);

                    if (method == null || method.ReturnType != typeof(string)) continue;

                    try
                    {
                        harmony.Patch(method, postfix: postfixMethod);
                        count++;
                        Log.Out($"[TitlesSystem] PlayerNamePatch: patched '{targetType.Name}.{methodName}'");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[TitlesSystem] PlayerNamePatch: could not patch '{targetType.Name}.{methodName}': {ex.Message}");
                    }

                    // Only patch one type per method name (EntityPlayer preferred).
                    break;
                }
            }

            if (count == 0)
                Log.Warning("[TitlesSystem] PlayerNamePatch: no compatible name method found — titles may not appear above player models via this path.");
            else
                Log.Out($"[TitlesSystem] PlayerNamePatch applied to {count} method(s).");
        }

        /// <summary>
        /// Postfix: sets the returned name to "[ShortTitle] OriginalName" using the
        /// player's stored original name (from <see cref="PlayerRankData.OriginalName"/>)
        /// rather than the method's own return value. This guarantees that rank-up
        /// events always produce a single, correctly-formatted prefix and never stack
        /// multiple brackets (e.g. "[Warlord] [Civ] Player").
        ///
        /// If the player has no rank data yet the result is left unchanged.
        /// </summary>
        public static void Postfix(EntityAlive __instance, ref string __result)
        {
            try
            {
                if (!(__instance is EntityPlayer)) return;
                if (string.IsNullOrEmpty(__result)) return;

                int entityId = GetEntityId(__instance);
                if (entityId < 0) return;

                ClientInfo clientInfo = GameApiCompat.GetClientInfoByEntityId(entityId);
                if (clientInfo == null) return;

                string playerId = GameApiCompat.GetPlayerId(clientInfo);
                if (string.IsNullOrEmpty(playerId)) return;

                string shortTitle = RankManager.Instance.GetPlayerShortTitle(playerId);
                if (string.IsNullOrEmpty(shortTitle)) return;

                // Use the stored original (un-prefixed) name as the base so that
                // rank changes never stack multiple prefixes.
                PlayerRankData data = RankManager.Instance.GetPlayerData(playerId);
                string baseName = (data != null && !string.IsNullOrEmpty(data.OriginalName))
                    ? data.OriginalName
                    : __result;

                __result = $"[{shortTitle}] {baseName}";
            }
            catch
            {
                // Best-effort: never interfere with the game if something goes wrong.
            }
        }

        private static int GetEntityId(object entity)
        {
            if (entity == null) return -1;
            try
            {
                var type = entity.GetType();

                // 7D2D entity classes use lowercase camelCase for public fields (e.g. entityId).
                var field = type.GetField("entityId");
                if (field != null && field.GetValue(entity) is int fid) return fid;

                var prop = type.GetProperty("entityId");
                if (prop != null && prop.GetValue(entity, null) is int pid) return pid;
            }
            catch { }
            return -1;
        }
    }
}
