using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public sealed class UdpMarkerReceiverWindow : EditorWindow
    {
        private int port = 5005;
        private UdpClient receiver;
        private readonly List<string> messages = new List<string>();
        private Vector2 scroll;
        private string status = "Not listening";

        [MenuItem("Tools/EEG Drone/UDP Marker Test Receiver")]
        public static void Open() { GetWindow<UdpMarkerReceiverWindow>("UDP Marker Receiver"); }

        private void OnEnable() { EditorApplication.update += Poll; }
        private void OnDisable()
        {
            EditorApplication.update -= Poll;
            StopListening();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Local test only. Binds 127.0.0.1; does not connect to an EEG device or synchronize EEG clocks.", MessageType.Info);
            using (new EditorGUI.DisabledScope(receiver != null))
                port = EditorGUILayout.IntField("Port", port);
            if (receiver == null)
            {
                if (GUILayout.Button("Start Listening")) StartListening();
            }
            else if (GUILayout.Button("Stop Listening")) StopListening();
            EditorGUILayout.LabelField(status);
            if (GUILayout.Button("Clear Received Messages")) messages.Clear();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string message in messages)
                EditorGUILayout.SelectableLabel(message, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(80f));
            EditorGUILayout.EndScrollView();
        }

        private void StartListening()
        {
            if (port < 1 || port > 65535) { status = "Port must be 1-65535"; return; }
            try
            {
                receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                receiver.Client.Blocking = false;
                status = "Listening on 127.0.0.1:" + port;
            }
            catch (Exception error)
            {
                receiver?.Dispose();
                receiver = null;
                status = "Cannot listen: " + error.Message;
            }
        }

        private void StopListening()
        {
            receiver?.Dispose();
            receiver = null;
            status = "Not listening";
        }

        private void Poll()
        {
            if (receiver == null) return;
            try
            {
                // Bound work per editor update so a busy socket cannot freeze Unity.
                for (int i = 0; i < 64 && receiver.Available > 0; i++)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    string json = Encoding.UTF8.GetString(receiver.Receive(ref remote));
                    messages.Add(DateTime.UtcNow.ToString("O") + " | " + remote + "\n" + json);
                    if (messages.Count > 200) messages.RemoveAt(0);
                    Repaint();
                }
            }
            catch (SocketException error)
            {
                if (error.SocketErrorCode == SocketError.WouldBlock) return;
                StopListening();
                status = "Receive failed: " + error.Message;
            }
        }
    }
}
