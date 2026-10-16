using System;

namespace EEGDroneExperiment
{
    [Serializable]
    public sealed class MarkerRecord
    {
        public int schema_version = 1;
        public string session_id;
        public int trial_id;
        public int sequence;
        public int marker;
        public string event_name;
        public double unity_time_s;
        public double trial_time_s;
        public float distance_m;
    }

    // Trial state independent of input, physics and networking.
    public sealed class TrialMarkers
    {
        public string SessionId { get; }
        public int TrialId { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasApproached { get; private set; }
        public bool HasNearMarker { get; private set; }
        public bool HasCollision { get; private set; }
        public double CollisionTimestamp { get; private set; } = double.NaN;
        private int sequence;
        private double startTime;

        public TrialMarkers(string sessionId) { SessionId = sessionId; }

        public MarkerRecord Start(double time, float distance)
        {
            if (IsRunning) return null;
            TrialId++;
            IsRunning = true;
            HasApproached = HasNearMarker = HasCollision = false;
            CollisionTimestamp = double.NaN;
            startTime = time;
            return Record(1, "TRIAL_START", time, distance);
        }

        public MarkerRecord Approach(double time, float distance)
        {
            if (!IsRunning || HasApproached) return null;
            HasApproached = true;
            return Record(2, "APPROACH_START", time, distance);
        }

        public MarkerRecord Near(double time, float distance, float threshold)
        {
            if (!IsRunning || !HasApproached || HasNearMarker || distance > threshold) return null;
            HasNearMarker = true;
            return Record(3, "NEAR_BUS", time, distance);
        }

        public MarkerRecord Collide(double time)
        {
            if (!IsRunning || HasCollision) return null;
            HasCollision = true;
            IsRunning = false;
            CollisionTimestamp = time;
            return Record(4, "BUS_COLLISION", time, 0f);
        }

        public MarkerRecord Abort(double time, float distance)
        {
            if (!IsRunning) return null;
            IsRunning = false;
            return Record(9, "TRIAL_ABORTED", time, distance);
        }

        private MarkerRecord Record(int code, string name, double time, float distance)
        {
            return new MarkerRecord {
                session_id = SessionId, trial_id = TrialId, sequence = ++sequence,
                marker = code, event_name = name, unity_time_s = time,
                trial_time_s = time - startTime, distance_m = distance
            };
        }
    }
}
