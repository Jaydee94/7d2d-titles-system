using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TitlesSystem.Commands;

namespace TitlesSystem.Patches
{
    /// <summary>
    /// Intercepts player chat and routes rank commands through ConsoleCmdRank.
    /// Registered manually via ApplyPatch (not via PatchAll) to avoid type-load
    /// failures when the game engine scans the assembly on startup.
    /// </summary>
    public static class ChatCommandPatch
    {
        /// <summary>
        /// Called from TitlesSystemMod.InitMod to patch every matching
        /// ChatMessageServer overload that exists in this server build.
        /// </summary>
        public static void ApplyPatch(Harmony harmony)
        {
            var prefixMethod = new HarmonyMethod(typeof(ChatCommandPatch), nameof(Prefix));
            int count = 0;

            foreach (var method in typeof(GameManager).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!string.Equals(method.Name, "ChatMessageServer", StringComparison.Ordinal)) continue;

                ParameterInfo[] p = method.GetParameters();
                if (p.Length != 7) continue;
                if (p[0].ParameterType != typeof(ClientInfo)) continue;
                if (p[3].ParameterType != typeof(string)) continue;

                harmony.Patch(method, prefix: prefixMethod);
                count++;
                Log.Out($"[TitlesSystem] ChatCommandPatch: patched overload '{method}'");
            }

            if (count == 0)
                Log.Warning("[TitlesSystem] ChatCommandPatch: no compatible ChatMessageServer overload found — 'rank' in chat will not work.");
            else
                Log.Out($"[TitlesSystem] ChatCommandPatch applied to {count} overload(s).");
        }

        public static bool Prefix(object[] __args)
        {
            if (__args == null || __args.Length < 4) return true;

            var cInfo = __args[0] as ClientInfo;
            object chatType = __args[1];
            string message = __args[3] as string;

            return !TryHandleRankChatCommand(cInfo, chatType, message);
        }

        private static bool TryHandleRankChatCommand(ClientInfo cInfo, object chatType, string message)
        {
            if (cInfo == null || string.IsNullOrWhiteSpace(message)) return false;

            // Only intercept normal player chat submissions.
            if (!IsInterceptableChatType(chatType))
                return false;

            var commandParams = ParseCommandParams(message);
            if (commandParams.Count == 0) return false;

            string command = commandParams[0];
            if (!string.Equals(command, "rank", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(command, "ranks", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(command, "title", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var sender = new CommandSenderInfo { RemoteClientInfo = cInfo };
                // ConsoleCmdRank.Execute expects parameters after the command name.
                // Example: "/rank top" should pass ["top"], not ["rank", "top"].
                var args = commandParams.Count > 1
                    ? commandParams.GetRange(1, commandParams.Count - 1)
                    : new List<string>();

                new ConsoleCmdRank().Execute(args, sender);
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] ChatCommandPatch error: {e.Message}");
                GameApiCompat.ChatMessageToClient(
                    cInfo,
                    Localization.Get("chat.command.failed", "[TitlesSystem] Command failed. Check server logs."));
            }

            // Command handled: suppress the raw command text from global chat.
            return true;
        }

        private static bool IsInterceptableChatType(object chatType)
        {
            if (chatType == null) return false;

            string name = chatType.ToString();
            if (string.IsNullOrEmpty(name)) return false;

            return string.Equals(name, "Global", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Friends", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Team", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ParseCommandParams(string message)
        {
            var parts = message.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i];
                if (i == 0 && token.StartsWith("/", StringComparison.Ordinal))
                    token = token.Substring(1);

                if (!string.IsNullOrEmpty(token))
                    result.Add(token);
            }

            return result;
        }
    }
}
