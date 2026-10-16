using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EEGDroneExperiment
{
    public sealed class ExperimentManager : MonoBehaviour
    {
        [Header("Trial")]
        [SerializeField] private DroneController drone;
        [SerializeField] private BoxCollider busCollider;
        [SerializeField, Min(0.01f)] private float nearDistance = 1f;
        [SerializeField] private bool showInstructions = true;
        [Tooltip("Keep this setting consistent across trials; the flash is an additional visual stimulus.")]
        [SerializeField] private bool playExplosion = true;
        [Header("UDP - configure BEFORE pressing Play")]
        [SerializeField] private bool enableUdp = false;
        [Tooltip("Use 127.0.0.1 for the built-in test receiver. Blank/invalid IP keeps local recording only.")]
        [SerializeField] private string destinationIp = "";
        [SerializeField, Range(1, 65535)] private int destinationPort = 5005;
        private TrialMarkers trial;
        private MarkerOutput output;
        private BoxCollider droneCollider;
        private Vector3 previousPosition;
        private float previousDistance;
        private bool ready;
        private bool quitting;
        private string status = "Waiting for initialization";

        public bool HasRecordedCollision => trial != null && trial.HasCollision;
        public double CollisionTimestamp => trial == null ? double.NaN : trial.CollisionTimestamp;
        public bool CanMove => ready && trial != null && trial.IsRunning && output != null && output.CanRecord;
        public bool PlayExplosion => playExplosion;

        // Required because this project disables both domain and scene reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BeginPlaySession()
        {
            foreach (var manager in UnityEngine.Object.FindObjectsByType<ExperimentManager>())
                manager.InitializeSession();
        }

        public void Configure(DroneController controller, BoxCollider target)
        {
            drone = controller;
            busCollider = target;
        }

        private void InitializeSession()
        {
            output?.Dispose();
            output = null;
            ready = false;
            quitting = false;
            string session = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture)
                + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            trial = new TrialMarkers(session);
            if (drone == null)
            {
                var drones = UnityEngine.Object.FindObjectsByType<DroneController>();
                if (drones.Length == 1) drone = drones[0];
            }
            if (busCollider == null)
            {
                var buses = UnityEngine.Object.FindObjectsByType<CollisionEventMarker>();
                if (buses.Length == 1) busCollider = buses[0].GetComponent<BoxCollider>();
            }
            droneCollider = drone != null ? drone.GetComponent<BoxCollider>() : null;
            if (drone == null || droneCollider == null || busCollider == null ||
                busCollider.GetComponentInParent<CollisionEventMarker>() == null)
            {
                Fail("Assign one Drone and a marked Bus BoxCollider on ExperimentManager.");
                return;
            }
            drone.Configure(this, false);
            drone.ResetForRun();
            Physics.SyncTransforms();
            if (!AxisAligned(droneCollider.transform) || !AxisAligned(busCollider.transform))
            {
                Fail("Marker distance requires axis-aligned box colliders. Reset Drone/Bus rotation or use multiples of 90 degrees.");
                return;
            }
            if (!droneCollider.enabled || !busCollider.enabled || droneCollider.isTrigger || busCollider.isTrigger)
            {
                Fail("Drone and Bus colliders must be enabled and must not be triggers.");
                return;
            }
            string folder = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ExperimentMarkers"))
                : Path.Combine(Application.persistentDataPath, "ExperimentMarkers");
            try { output = new MarkerOutput(Path.Combine(folder, session + ".jsonl"), enableUdp, destinationIp, destinationPort); }
            catch (Exception error) { Fail("Cannot create marker log: " + error.Message); return; }
            ready = true;
            status = "Press ENTER to start a trial";
            Debug.Log("Marker log: " + output.LogPath + (output.UdpActive ? " | UDP enabled" : " | Local recording only"), this);
        }

        private void Update()
        {
            if (output != null)
            {
                while (output.TryGetError(out string error)) Debug.LogWarning(error, this);
                if (!output.CanRecord && ready)
                {
                    AbortTrial();
                    Fail("Local recording failed. Stop Play and check the Console before continuing.");
                }
            }
            if (!ready || !Application.isFocused || Keyboard.current == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame && trial.IsRunning) AbortTrial();
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                StartTrial();
        }

        private void StartTrial()
        {
            if (trial.IsRunning) return;
            drone.ResetForRun();
            Physics.SyncTransforms();
            float distance = SurfaceDistance();
            if (distance <= nearDistance || nearDistance <= 0f || float.IsNaN(nearDistance) || float.IsInfinity(nearDistance))
            {
                status = "Start must be farther from the bus than Near Distance. Stop Play and check settings.";
                Debug.LogError(status, this);
                return;
            }
            foreach (var effect in UnityEngine.Object.FindObjectsByType<CollisionExplosion>())
                Destroy(effect.gameObject);
            previousPosition = drone.PhysicsPosition;
            previousDistance = distance;
            Publish(trial.Start(Time.realtimeSinceStartupAsDouble, distance));
            status = "";
        }

        private void LateUpdate()
        {
            if (CanMove) ObserveMotion(Time.realtimeSinceStartupAsDouble, SurfaceDistance());
        }

        private void ObserveMotion(double timestamp, float distance)
        {
            Vector3 position = drone.PhysicsPosition;
            Vector3 travel = position - previousPosition;
            // Actual displacement toward the target, not merely a held key.
            bool approaching = distance < previousDistance - 0.00001f &&
                Vector3.Dot(travel, busCollider.bounds.center - previousPosition) > 0f;
            if (approaching) Publish(trial.Approach(timestamp, distance));
            Publish(trial.Near(timestamp, distance, nearDistance));
            previousPosition = position;
            previousDistance = distance;
        }

        public bool IsTarget(Collider collider) => collider == busCollider;

        public bool RecordBusCollision(double timestamp)
        {
            if (!CanMove) return false;
            // Preserve ordering when several thresholds are crossed in one physics step;
            // do not invent earlier event timestamps.
            ObserveMotion(timestamp, 0f);
            MarkerRecord marker = trial.Collide(timestamp);
            if (marker == null) return false;
            Debug.Log("BUS_COLLISION", this);
            Debug.Log("Timestamp: " + timestamp.ToString("F6", CultureInfo.InvariantCulture), this);
            Publish(marker);
            drone.StopMotion();
            status = "Trial complete. Press ENTER for the next trial";
            return true;
        }

        private void AbortTrial()
        {
            if (trial == null || !trial.IsRunning) return;
            Publish(trial.Abort(Time.realtimeSinceStartupAsDouble, SurfaceDistance()));
            drone.StopMotion();
            status = "Trial aborted. Press ENTER for the next trial";
        }

        private void Publish(MarkerRecord marker)
        {
            if (marker == null) return;
            output?.Write(JsonUtility.ToJson(marker));
            Debug.Log("MARKER " + marker.marker + " | trial " + marker.trial_id + " | seq " + marker.sequence
                + " | t=" + marker.unity_time_s.ToString("F6", CultureInfo.InvariantCulture), this);
        }

        private float SurfaceDistance()
        {
            if (droneCollider == null || busCollider == null) return -1f;
            Bounds a = droneCollider.bounds;
            // Rigidbody interpolation can make the rendered Transform trail physics.
            // Measure the physical box location used by the collision simulation.
            a.center += drone.PhysicsPosition - droneCollider.transform.position;
            Bounds b = busCollider.bounds;
            Vector3 gap = Vector3.Max(Vector3.zero, Vector3.Max(a.min - b.max, b.min - a.max));
            return gap.magnitude;
        }

        private static bool AxisAligned(Transform target)
        {
            return AlignedAxis(target.right) && AlignedAxis(target.up) && AlignedAxis(target.forward);
        }

        private static bool AlignedAxis(Vector3 axis)
        {
            return Mathf.Max(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)) > 0.99999f;
        }

        private void Fail(string message)
        {
            ready = false;
            status = message;
            Debug.LogError(message, this);
        }

        private void OnGUI()
        {
            if (!showInstructions || string.IsNullOrEmpty(status)) return;
            GUI.Box(new Rect(15f, 15f, 620f, 85f), status + "\nWASD move | Space up | Left Ctrl down | R abort\n"
                + (output != null && output.UdpActive ? "UDP enabled (delivery not confirmed)" : "Local recording only"));
        }

        private void CloseSession()
        {
            if (quitting) return;
            quitting = true;
            AbortTrial();
            ready = false;
            output?.Dispose();
            output = null;
        }

        private void OnApplicationQuit() { CloseSession(); }
        private void OnDisable() { CloseSession(); }
    }
}
