using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EEGDroneExperiment
{
    // UTF-8 JSONL locally; one JSON object per UDP datagram. No automatic retry.
    public sealed class MarkerOutput : IDisposable
    {
        private StreamWriter log;
        private UdpClient udp;
        private IPEndPoint destination;
        private volatile bool disposed;
        private readonly ConcurrentQueue<string> errors = new ConcurrentQueue<string>();
        public bool CanRecord { get; private set; } = true;
        public bool UdpActive => udp != null && !disposed;
        public string LogPath { get; }

        public MarkerOutput(string path, bool enableUdp, string ip, int port)
        {
            LogPath = path;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            log = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(false)) { AutoFlush = true };
            if (!enableUdp) return;
            if (!IPAddress.TryParse(ip, out IPAddress address) || port < 1 || port > 65535)
            {
                errors.Enqueue("UDP disabled: enter a numeric IP address and a port from 1 to 65535. Local recording remains active.");
                return;
            }
            try
            {
                udp = new UdpClient(address.AddressFamily);
                destination = new IPEndPoint(address, port);
            }
            catch (Exception error)
            {
                errors.Enqueue("UDP setup failed: " + error.Message);
                udp?.Dispose();
                udp = null;
            }
        }

        public void Write(string json)
        {
            if (disposed) return;
            try { log.WriteLine(json); }
            catch (Exception error)
            {
                CanRecord = false;
                errors.Enqueue("Local marker recording failed: " + error.Message);
            }
            if (udp != null) _ = SendAsync(Encoding.UTF8.GetBytes(json));
        }

        private async Task SendAsync(byte[] bytes)
        {
            try { await udp.SendAsync(bytes, bytes.Length, destination).ConfigureAwait(false); }
            catch (Exception error)
            {
                if (!disposed) errors.Enqueue("UDP send failed: " + error.Message);
            }
        }

        public bool TryGetError(out string error) => errors.TryDequeue(out error);

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            udp?.Dispose();
            try { log?.Dispose(); }
            catch (IOException) { }
        }
    }
}
