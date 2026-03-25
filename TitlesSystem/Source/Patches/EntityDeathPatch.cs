using HarmonyLib;
using System;
using TitlesSystem;

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
                if (_dmResponse.Source == null) return;

                // Track zombie kills
                if (__instance is EntityZombie)
                {
                    int killerEntityId = GameApiCompat.GetKillerEntityId(_dmResponse.Source);
                    if (killerEntityId < 0) return;

                    // Confirm the killer is a player
                    EntityPlayer killerPlayer =
                        GameManager.Instance?.World?.Players?.dict?.TryGetValue(killerEntityId, out var p) == true ? p : null;

                    if (killerPlayer == null) return;

                    // Resolve ClientInfo to get the stable platform player ID
                    ClientInfo clientInfo = GameApiCompat.GetClientInfoByEntityId(killerEntityId);
                    if (clientInfo == null) return;

                    string killerPlayerId = GameApiCompat.GetPlayerId(clientInfo);
                    if (string.IsNullOrEmpty(killerPlayerId)) return;

                    // Extract weapon info from damage source
                    string weaponId = ExtractWeaponId(_dmResponse);

                    RankManager.Instance.OnZombieKilled(killerEntityId, killerPlayerId, weaponId);
                }
                // Track player deaths
                else if (__instance is EntityPlayer deadPlayer)
                {
                    ClientInfo deadClientInfo = GameApiCompat.GetClientInfoByEntityId(deadPlayer.entityId);
                    if (deadClientInfo == null) return;

                    string deadPlayerId = GameApiCompat.GetPlayerId(deadClientInfo);
                    if (string.IsNullOrEmpty(deadPlayerId)) return;

                    RankManager.Instance.OnPlayerDied(deadPlayerId);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] EntityDeathPatch error: {e.Message}");
            }
        }

        /// <summary>
        /// Extract weapon name from a DamageResponse's source.
        /// Returns "unknown" if unable to determine.
        /// </summary>
        private static string ExtractWeaponId(DamageResponse dmResponse)
        {
            try
            {
                if (dmResponse.Source is EntityPlayer player && player.inventory != null)
                {
                    // Try to get the held item
                    var heldItem = player.inventory.GetHeldItem();
                    if (heldItem != null && !string.IsNullOrEmpty(heldItem.Name))
                        return heldItem.Name;
                }

                // Fallback: try to extract from damage type or source name
                if (dmResponse.Source is EntityPlayer p2 && !string.IsNullOrEmpty(p2.entityName))
                    return "melee"; // Assume melee if no item found

                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
