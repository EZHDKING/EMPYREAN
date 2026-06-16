// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Online;

namespace osu.Game.Empyrean
{
    /// <summary>
    /// An <see cref="EndpointConfiguration"/> pointed at an arbitrary host, so EMPYREAN can connect
    /// to private servers (e.g. dev.ppy.sh or any custom server). The chosen host is persisted to a
    /// JSON file and read at startup by the game's endpoint factory — switching servers requires a
    /// restart because the API connection is established once during boot.
    /// </summary>
    public class EmpyreanEndpointConfiguration : EndpointConfiguration
    {
        public EmpyreanEndpointConfiguration(string host)
        {
            // Normalise: accept "dev.ppy.sh" or "https://dev.ppy.sh".
            host = (host ?? string.Empty).Trim().TrimEnd('/');
            if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                host = host.Substring(7);
            else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                host = host.Substring(8);

            if (string.IsNullOrWhiteSpace(host))
                host = "dev.ppy.sh";

            string root = $"https://{host}";

            WebsiteUrl = APIUrl = root;
            // osu-web default OAuth client used by lazer/dev servers (public client id 5).
            APIClientSecret = @"FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk";
            APIClientID = "5";
            SpectatorUrl = $"https://{host}/signalr/spectator";
            MultiplayerUrl = $"https://{host}/signalr/multiplayer";
            MetadataUrl = $"https://{host}/signalr/metadata";
        }
    }

    /// <summary>
    /// Persists the chosen server host so it survives restarts.
    /// </summary>
    public static class EmpyreanServerStore
    {
        private const string filename = "empyrean-server.json";

        public static string LoadHost(Storage storage)
        {
            try
            {
                if (storage == null || !storage.Exists(filename))
                    return null;

                using var stream = storage.GetStream(filename, FileAccess.Read);
                using var reader = new StreamReader(stream);
                var data = JsonConvert.DeserializeObject<ServerData>(reader.ReadToEnd());
                return data?.Host;
            }
            catch (Exception ex)
            {
                Logger.Log($"EMPYREAN: failed to load server host: {ex.Message}", LoggingTarget.Runtime);
                return null;
            }
        }

        public static void SaveHost(Storage storage, string host)
        {
            try
            {
                if (storage == null)
                    return;

                string json = JsonConvert.SerializeObject(new ServerData { Host = host }, Formatting.Indented);
                using var stream = storage.CreateFileSafely(filename);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
            catch (Exception ex)
            {
                Logger.Log($"EMPYREAN: failed to save server host: {ex.Message}", LoggingTarget.Runtime);
            }
        }

        [Serializable]
        private class ServerData
        {
            public string Host { get; set; } = string.Empty;
        }
    }
}
