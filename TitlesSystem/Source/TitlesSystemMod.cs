using HarmonyLib;
using System;
using System.Reflection;

namespace TitlesSystem
{
    /// <summary>
    /// Entry point for the TitlesSystem mod.
    /// Implements IModApi so the 7DTD engine can discover and load this assembly.
    /// </summary>
    public class TitlesSystemMod : IModApi
    {
        private Harmony _harmony;

        // ------------------------------------------------------------------ //
        //  IModApi implementation
        // ------------------------------------------------------------------ //

        public void InitMod(Mod _modInstance)
        {
            Log.Out("[TitlesSystem] Initializing Titles System mod...");

            try
            {
                // Initialize the rank manager with the mod's config directory
                RankManager.Instance.Initialize(_modInstance.Path);

                // Apply Harmony patches for types that use [HarmonyPatch] attributes (e.g. EntityDeathPatch)
                _harmony = new Harmony("com.jaydee94.titlesystem");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[TitlesSystem] Harmony patches applied.");

                // Chat command intercept is registered manually to avoid type-load
                // failures when the game engine iterates the assembly on startup.
                try
                {
                    TitlesSystem.Patches.ChatCommandPatch.ApplyPatch(_harmony);
                }
                catch (Exception chatPatchEx)
                {
                    Log.Warning($"[TitlesSystem] Chat command intercept could not be applied: {chatPatchEx.Message}");
                }

                // Player name display patch: intercepts GetPlayerName() (and similar methods)
                // so that the rank title appears before the player name in all name contexts,
                // including the 3D overhead display above the player model.
                try
                {
                    TitlesSystem.Patches.PlayerNamePatch.ApplyPatch(_harmony);
                }
                catch (Exception namePatchEx)
                {
                    Log.Warning($"[TitlesSystem] Player name patch could not be applied: {namePatchEx.Message}");
                }

                // Subscribe to game events
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
                ModEvents.PlayerDisconnected.RegisterHandler(OnPlayerDisconnected);

                Log.Out("[TitlesSystem] Titles System mod initialized successfully.");
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] Failed to initialize mod: {e}");
            }
        }

        // ------------------------------------------------------------------ //
        //  Game Event Handlers
        // ------------------------------------------------------------------ //

        private void OnGameStartDone(ref ModEvents.SGameStartDoneData _data)
        {
            try
            {
                RankManager.Instance.OnGameStartDone();
                Log.Out("[TitlesSystem] Game started — rank data path configured.");
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] OnGameStartDone error: {e.Message}");
            }
        }

        private void OnPlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData _data)
        {
            try
            {
                if (_data.ClientInfo == null) return;
                RankManager.Instance.OnPlayerSpawned(_data.ClientInfo);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] OnPlayerSpawnedInWorld error: {e.Message}");
            }
        }

        private void OnPlayerDisconnected(ref ModEvents.SPlayerDisconnectedData _data)
        {
            try
            {
                if (_data.ClientInfo == null) return;
                RankManager.Instance.OnPlayerDisconnected(_data.ClientInfo);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] OnPlayerDisconnected error: {e.Message}");
            }
        }
    }
}
