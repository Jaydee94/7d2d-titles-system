using HarmonyLib;
using System;

namespace TitlesSystem.Patches
{
    /// <summary>
    /// Harmony patch on <see cref="EntityAlive.Kill"/> to detect zombie kills
    /// made by players and forward them to <see cref="RankManager"/>.
    ///
    /// This runs server-side only (the mod is server-side), so no client-side
    /// guard is needed beyond the entity type checks.
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), "Kill")]
    public static class EntityDeathPatch
    {
        /// <summary>
        /// Postfix runs after the entity's Kill method — the entity is already
        /// marked dead at this point.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(EntityAlive __instance, DamageResponse _dmResponse)
        {
            try
            {
                // Only track kills of actual zombies (excludes animals, players, etc.)
                if (!(__instance is EntityZombie)) return;

                // A null or missing source means environmental death — skip
                if (_dmResponse.Source == null) return;

                int killerEntityId = _dmResponse.Source.BoundEntityId;
                if (killerEntityId < 0) return;

                // Confirm the killer is a player
                EntityPlayer killerPlayer =
                    GameManager.Instance?.World?.Players?.dict?.TryGetValue(killerEntityId, out var p) == true ? p : null;

                if (killerPlayer == null) return;

                // Resolve ClientInfo to get the stable platform player ID
                ClientInfo clientInfo = ConnectionManager.Instance?.Clients?.GetForEntityId(killerEntityId);
                if (clientInfo == null) return;

                RankManager.Instance.OnZombieKilled(killerEntityId, clientInfo.playerId);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] EntityDeathPatch error: {e.Message}");
            }
        }
    }
}
