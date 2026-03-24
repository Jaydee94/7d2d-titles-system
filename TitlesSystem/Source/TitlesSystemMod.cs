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

                // Apply all Harmony patches in this assembly
                _harmony = new Harmony("com.jaydee94.titlesystem");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[TitlesSystem] Harmony patches applied.");

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

        private void OnGameStartDone()
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

        private void OnPlayerSpawnedInWorld(ClientInfo _cInfo, RespawnType _respawnReason, Vector3i _pos)
        {
            try
            {
                if (_cInfo == null) return;
                RankManager.Instance.OnPlayerSpawned(_cInfo);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] OnPlayerSpawnedInWorld error: {e.Message}");
            }
        }

        private void OnPlayerDisconnected(ClientInfo _cInfo, bool _bShutdown)
        {
            try
            {
                if (_cInfo == null) return;
                RankManager.Instance.OnPlayerDisconnected(_cInfo);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] OnPlayerDisconnected error: {e.Message}");
            }
        }
    }
}
