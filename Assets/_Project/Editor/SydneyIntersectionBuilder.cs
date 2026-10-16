using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    // Static scenery only: no traffic simulation or changes to the experiment event.
    public static class SydneyIntersectionBuilder
    {
        private const string RootName = "SydneyIntersection";
        private const string MaterialsPath = "Assets/_Project/Materials";

        [MenuItem("Tools/EEG Drone/Add Sydney Intersection and Light Rail")]
        public static void AddToScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before adding the intersection.");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            CollisionEventMarker bus = null;
            foreach (var marker in Object.FindObjectsByType<CollisionEventMarker>())
                if (marker.gameObject.scene == scene) { bus = marker; break; }
            if (bus == null)
            {
                Debug.LogWarning("Open DroneBusCollision or build the experiment scene first.");
                return;
            }
            Build(bus.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Sydney intersection added. The light rail waits at a fixed red signal. Save with Ctrl+S.");
        }

        public static void Build(GameObject bus)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader is missing; intersection was not created.");
                return;
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Sydney intersection");
            foreach (var obj in bus.scene.GetRootGameObjects())
                if (obj.name == RootName) Undo.DestroyObjectImmediate(obj);
            var root = new GameObject(RootName);
            // Keep the intersection just before the bus's original front face.
            root.transform.position = new Vector3(bus.transform.position.x, 0f, bus.transform.position.z - 8f);
            // Extend the existing approach road, preserving its width and elevation.
            Transform ground = bus.transform.parent != null ? bus.transform.parent.Find("Ground") : null;
            if (ground != null)
            {
                Undo.RecordObject(ground, "Extend approach road");
                Vector3 roadScale = ground.localScale;
                roadScale.z = Mathf.Max(roadScale.z, 140f);
                ground.localScale = roadScale;
            }
            Transform streets = Group(root.transform, "Streets and Crossings");
            var asphalt = GetMaterial("CityAsphalt", new Color(0.19f, 0.21f, 0.23f), shader);
            var paving = GetMaterial("CityPaving", new Color(0.63f, 0.59f, 0.53f), shader);
            var white = GetMaterial("TramWhite", new Color(0.92f, 0.93f, 0.91f), shader);
            var red = GetMaterial("TramRed", new Color(0.76f, 0.035f, 0.065f), shader);
            var dark = GetMaterial("TramDark", new Color(0.025f, 0.035f, 0.045f), shader);
            var glass = GetMaterial("TramGlass", new Color(0.035f, 0.10f, 0.14f), shader);
            var steel = GetMaterial("RailSteel", new Color(0.42f, 0.46f, 0.48f), shader);
            var redLamp = GetMaterial("SignalRedOn", new Color(1f, 0.025f, 0.01f), shader, true);
            var greenLamp = GetMaterial("SignalGreenOn", new Color(0.04f, 0.95f, 0.30f), shader, true);
            var offLamp = GetMaterial("SignalOff", new Color(0.07f, 0.08f, 0.075f), shader);
            var sandstone = GetMaterial("CitySandstone", new Color(0.65f, 0.52f, 0.37f), shader);
            var office = GetMaterial("CityOffice", new Color(0.32f, 0.39f, 0.44f), shader);

            Box(streets, "Cross street", new Vector3(0f, 0.005f, 0f), new Vector3(140f, 0.07f, 6f), asphalt, true);
            // Cover previous lane dashes inside the junction without changing Ground.
            Box(streets, "Junction surface", new Vector3(0f, 0.005f, 0f), new Vector3(12f, 0.07f, 6f), asphalt);
            foreach (int side in new[] { -1, 1 })
            {
                foreach (int end in new[] { -1, 1 })
                {
                    Box(streets, "Corner pavement", new Vector3(side * 38.25f, 0.06f, end * 4.5f), new Vector3(63.5f, 0.24f, 3f), paving, true);
                    Box(streets, "Approach pavement", new Vector3(side * 7.7f, 0.06f, end * 32f), new Vector3(3f, 0.24f, 52f), paving, true);
                }
                // Paired pedestrian crossing boundaries leave the rails visible.
                foreach (float edge in new[] { -0.65f, 0.65f })
                {
                    Box(streets, "Pedestrian crossing", new Vector3(side * 7.25f + edge, 0.055f, 0f), new Vector3(0.12f, 0.015f, 6f), white);
                    Box(streets, "Pedestrian crossing", new Vector3(0f, 0.055f, side * 2.25f + edge), new Vector3(12f, 0.015f, 0.12f), white);
                }
                Box(streets, "Tram stop line", new Vector3(side * 8.5f, 0.057f, side * -1.5f), new Vector3(0.25f, 0.015f, 2.8f), white);
            }
            Transform rails = Group(root.transform, "Light Rail Tracks");
            foreach (float track in new[] { -1.5f, 1.5f })
                foreach (float offset in new[] { -0.72f, 0.72f })
                    Box(rails, "Embedded rail", new Vector3(0f, 0.065f, track + offset), new Vector3(140f, 0.035f, 0.07f), steel);

            Transform tram = Group(root.transform, "Light Rail - Waiting at Red");
            tram.localPosition = new Vector3(10.5f, 0.09f, -1.5f);
            // Curved nose extends 1 m forward, still behind the x=8.5 stop line.
            for (int car = 0; car < 4; car++)
            {
                float x = 3.1f + car * 6.6f;
                float bodyX = x + (car == 0 ? 0.725f : car == 3 ? -0.725f : 0f);
                float bodyLength = car == 0 || car == 3 ? 4.75f : 6.2f;
                Box(tram, "Carriage " + (car + 1), new Vector3(bodyX, 1.6f, 0f), new Vector3(bodyLength, 2.8f, 2.5f), white, true);
                Box(tram, "Red lower panel", new Vector3(bodyX, 0.75f, 0f), new Vector3(bodyLength, 0.95f, 2.52f), red);
                Box(tram, "Roof", new Vector3(bodyX, 3.03f, 0f), new Vector3(bodyLength, 0.16f, 2.5f), red);
                Box(tram, "Roof equipment", new Vector3(x, 3.21f, 0f), new Vector3(3.5f, 0.25f, 1.7f), steel);
                foreach (int side in new[] { -1, 1 })
                {
                    for (int window = car == 0 ? 1 : 0; window < (car == 3 ? 3 : 4); window++)
                        Box(tram, "Window", new Vector3(x - 2.25f + window * 1.5f, 2.12f, side * 1.265f), new Vector3(1.28f, 1.18f, 0.035f), glass);
                    Box(tram, "Passenger door", new Vector3(x + 0.7f, 1.57f, side * 1.29f), new Vector3(1.2f, 2.35f, 0.04f), dark);
                    Box(tram, "Door glass", new Vector3(x + 0.7f, 1.93f, side * 1.32f), new Vector3(1.02f, 1.45f, 0.025f), glass);
                    Box(tram, "Door seam", new Vector3(x + 0.7f, 1.6f, side * 1.34f), new Vector3(0.035f, 2.2f, 0.025f), white);
                    foreach (float axle in new[] { -1.95f, 1.95f })
                        Box(tram, "Wheel skirt", new Vector3(x + axle, 0.34f, side * 1.19f), new Vector3(1.2f, 0.42f, 0.17f), dark);
                }
                if (car < 3)
                    for (int fold = 0; fold < 5; fold++)
                        Box(tram, "Articulated joint", new Vector3(x + 3.15f + fold * 0.085f, 1.65f, 0f), new Vector3(0.045f, 2.65f, 2.3f), dark);
            }
            SydneyTramDetails.AddCab(tram, false, red, glass, white);
            SydneyTramDetails.AddCab(tram, true, red, glass, white);
            // Side destination signs remain readable without floating over the curved glass.
            foreach (int side in new[] { -1, 1 })
            {
                Box(tram, "Destination board", new Vector3(2.4f, 2.84f, side * 1.27f), new Vector3(1.8f, 0.25f, 0.04f), dark);
                Label(tram, "L2  CIRCULAR QUAY", new Vector3(2.4f, 2.84f, side * 1.30f), side == -1 ? 0f : 180f, 1.65f, 0.16f, Color.yellow);
                Box(tram, "Headlight", new Vector3(-0.76f, 0.95f, side * 0.58f), new Vector3(0.08f, 0.14f, 0.25f), white);
                Box(tram, "Tail light", new Vector3(26.76f, 0.95f, side * 0.58f), new Vector3(0.08f, 0.14f, 0.25f), redLamp);
            }
            SydneyTramDetails.AddStop(root.transform, red, white, dark, paving);

            Transform signals = Group(root.transform, "Traffic Signals - Static");
            Signal(signals, new Vector3(8.1f, 0f, -3.65f), 90f, true, dark, steel, redLamp, greenLamp, offLamp);
            Signal(signals, new Vector3(-8.1f, 0f, 3.65f), -90f, true, dark, steel, redLamp, greenLamp, offLamp);
            Signal(signals, new Vector3(-6.35f, 0f, -3.5f), 0f, false, dark, steel, redLamp, greenLamp, offLamp);
            Signal(signals, new Vector3(6.35f, 0f, 3.5f), 180f, false, dark, steel, redLamp, greenLamp, offLamp);
            // Observer-facing red repeater makes the tram's waiting state clear.
            Signal(signals, new Vector3(9f, 0f, -3.65f), 0f, true, dark, steel, redLamp, greenLamp, offLamp);

            Transform buildings = Group(root.transform, "City Buildings");
            foreach (int side in new[] { -1, 1 })
                foreach (int end in new[] { -1, 1 })
                    for (int i = 0; i < 3; i++)
                    {
                        float x = side * (15f + i * 10f);
                        float z = end * 12f;
                        float height = 6f + i * 2f;
                        GameObject building = Box(buildings, "City block", new Vector3(x, height / 2f, z), new Vector3(8f, height, 10f), i == 0 ? sandstone : office, true);
                        if (side == -1 && end == 1 && i == 0)
                            WooliesStoreBuilder.Build(building);
                        for (float y = 2f; y < height - 1f; y += 3f)
                            for (int column = -1; column <= 1; column++)
                                Box(buildings, "Street window", new Vector3(x + column * 2.4f, y, z - end * 5.02f), new Vector3(1.5f, 1.8f, 0.035f), glass);
                        Box(buildings, "Shop awning", new Vector3(x, 2.8f, z - end * 5.7f), new Vector3(8.2f, 0.15f, 1.4f), dark);
                    }
            SydneyStreetscapeBuilder.Build(root.transform);
            SydneyPedestrianBuilder.Build(root.transform);
            Undo.RegisterCreatedObjectUndo(root, "Create Sydney intersection");
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
        }

        private static Transform Group(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static void Signal(Transform parent, Vector3 position, float yaw, bool stop,
            Material dark, Material steel, Material red, Material green, Material off)
        {
            Transform pole = Group(parent, stop ? "Tram signal RED" : "Road signal GREEN");
            pole.localPosition = position;
            pole.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(pole, "Pole", new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 3.6f, 0.12f), steel);
            Box(pole, "Signal housing", new Vector3(0f, 3.2f, -0.1f), new Vector3(0.48f, 1.25f, 0.3f), dark);
            for (int i = 0; i < 3; i++)
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lamp.name = i == 0 ? "Red" : i == 1 ? "Amber off" : "Green";
                lamp.transform.SetParent(pole, false);
                lamp.transform.localPosition = new Vector3(0f, 3.58f - i * 0.38f, -0.27f);
                lamp.transform.localScale = new Vector3(0.29f, 0.29f, 0.07f);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                lamp.GetComponent<Renderer>().sharedMaterial = i == 0 && stop ? red : i == 2 && !stop ? green : off;
            }
            if (stop)
            {
                Box(pole, "Tram plate", new Vector3(0f, 2.3f, -0.1f), new Vector3(0.75f, 0.25f, 0.08f), dark);
                Label(pole, "TRAM", new Vector3(0f, 2.3f, -0.15f), 0f, 0.65f, 0.17f, Color.white);
            }
        }

        private static void Label(Transform parent, string value, Vector3 position, float yaw, float width, float height, Color color)
        {
            Transform label = Group(parent, value);
            label.localPosition = position;
            label.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = label.gameObject.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 64;
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            Vector3 bounds = label.GetComponent<MeshRenderer>().localBounds.size;
            if (bounds.x > 0f && bounds.y > 0f)
                label.localScale = Vector3.one * Mathf.Min(width / bounds.x, height / bounds.y);
        }

        private static Material GetMaterial(string name, Color color, Shader shader, bool emission = false)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            string path = MaterialsPath + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
