using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EEGDroneExperiment.Editor
{
    // Also compiled into a standalone test runner for verification without Unity UI.
    public static class MarkerChecks
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/EEG Drone/Run Marker Checks")]
        public static void RunFromEditor()
        {
            string directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../Temp/MarkerVerification", "run_" + Guid.NewGuid().ToString("N")));
            try
            {
                Run(directory);
                UnityEngine.Debug.Log("All marker state, local log and loopback UDP checks passed.");
            }
            catch (Exception error) { UnityEngine.Debug.LogException(error); }
        }
#endif

        public static void Run(string temporaryDirectory)
        {
            Directory.CreateDirectory(temporaryDirectory);
            var state = new TrialMarkers("verification");
            var records = new List<MarkerRecord>();
            Check(state.Approach(0, 5) == null && state.Near(0, 0.5f, 1) == null && state.Collide(0) == null,
                "No events before Enter/start");
            records.Add(state.Start(10, 10));
            Check(state.Start(11, 10) == null, "Repeated Enter does not restart active trial");
            Check(state.Near(11, 0.5f, 1) == null, "Near marker waits for approach");
            records.Add(state.Approach(12, 9));
            Check(state.Approach(13, 8) == null, "Approach occurs once");
            Check(state.Near(14, 1.01f, 1) == null, "Outside threshold is not near");
            records.Add(state.Near(15, 1f, 1));
            Check(state.Near(16, 0.9f, 1) == null, "Near marker occurs once");
            records.Add(state.Collide(17));
            Check(state.Collide(18) == null && !state.IsRunning, "Collision ends trial and cannot repeat");
            for (int i = 0; i < 4; i++)
                Check(records[i].marker == i + 1 && records[i].sequence == i + 1 && records[i].trial_id == 1,
                    "Ordered markers 1-4 with sequence and trial identifiers");
            Check(records[3].unity_time_s == 17 && records[3].trial_time_s == 7, "Source timestamp preserved");
            Check(state.Start(20, 10).trial_id == 2 && !state.HasCollision && !state.HasNearMarker && !state.HasApproached,
                "Next trial resets flags");
            Check(state.Abort(21, 8).marker == 9 && state.Abort(22, 8) == null, "Abort occurs once");
            Check(state.Start(30, 10).sequence == 7, "Sequence continues across trials");
            var newSession = new TrialMarkers("next-session");
            Check(newSession.Start(100, 10).sequence == 1, "New Play session starts fresh");

            string localPath = Path.Combine(temporaryDirectory, "local.jsonl");
            using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                int port = ((IPEndPoint)listener.Client.LocalEndPoint).Port;
                using (var output = new MarkerOutput(localPath, false, "127.0.0.1", port))
                {
                    output.Write("{\"marker\":1}");
                    Check(!output.UdpActive && listener.Available == 0, "UDP off sends nothing");
                }
                Check(File.ReadAllLines(localPath).Length == 1, "Local-only mode persists marker");
                string udpPath = Path.Combine(temporaryDirectory, "udp.jsonl");
                using (var output = new MarkerOutput(udpPath, true, "127.0.0.1", port))
                {
                    listener.Client.ReceiveTimeout = 2000;
                    const string payload = "{\"marker\":3,\"sequence\":3,\"event_name\":\"NEAR_BUS\"}";
                    output.Write(payload);
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    string received = Encoding.UTF8.GetString(listener.Receive(ref remote));
                    Check(received == payload, "Loopback UDP payload matches exactly");
                    using (var stream = new FileStream(udpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                        Check(reader.ReadToEnd().Trim() == payload, "Local log matches datagram");
                    Check(!output.TryGetError(out _), "No sender errors");
                }
            }
            using (var output = new MarkerOutput(Path.Combine(temporaryDirectory, "invalid.jsonl"), true, "", 5005))
            {
                output.Write("{\"marker\":1}");
                Check(!output.UdpActive && output.CanRecord && output.TryGetError(out _), "Missing IP falls back to local log");
            }
            using (var output = new MarkerOutput(Path.Combine(temporaryDirectory, "bad-port.jsonl"), true, "127.0.0.1", 0))
                Check(!output.UdpActive && output.CanRecord && output.TryGetError(out _), "Invalid port falls back to local log");
        }

        private static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("FAILED: " + message);
            Console.WriteLine("PASS: " + message);
        }
    }
}
