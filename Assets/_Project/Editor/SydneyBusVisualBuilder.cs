using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    // A lightweight Sydney-inspired bus. The original bus collider stays intact.
    public static class SydneyBusVisualBuilder
    {
        private const string VisualName = "SydneyBusVisual";

        [MenuItem("Tools/EEG Drone/Update Bus to Sydney Style")]
        public static void UpdateSceneBus()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before updating the bus.");
                return;
            }
            var buses = Object.FindObjectsByType<CollisionEventMarker>();
            int count = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update Sydney bus visuals");
            foreach (var marker in buses)
            {
                if (marker.gameObject.scene != EditorSceneManager.GetActiveScene())
                    continue;
                Build(marker.gameObject);
                count++;
            }
            Undo.CollapseUndoOperations(undoGroup);
            if (count == 0)
                Debug.LogWarning("Open DroneBusCollision or build the experiment scene first.");
            else
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("Sydney bus visuals updated. Save the scene with Ctrl+S.");
            }
        }

        public static void Build(GameObject bus)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader is missing.");
                return;
            }
            var previous = bus.transform.Find(VisualName);
            if (previous != null)
                Undo.DestroyObjectImmediate(previous.gameObject);
            var originalRenderer = bus.GetComponent<MeshRenderer>();
            if (originalRenderer != null)
            {
                Undo.RecordObject(originalRenderer, "Hide placeholder bus");
                originalRenderer.enabled = false;
            }
            var visual = new GameObject(VisualName);
            visual.transform.SetParent(bus.transform, false);
            // Dimensions below are fractions of the existing cube, preserving its bounds.
            var white = Material("BusWhite", new Color(0.9f, 0.94f, 0.96f), shader);
            var blue = Material("BusSkyBlue", new Color(0.16f, 0.58f, 0.83f), shader);
            var navy = Material("BusNavy", new Color(0.025f, 0.12f, 0.23f), shader);
            var glass = Material("BusGlass", new Color(0.025f, 0.07f, 0.1f), shader);
            var rubber = Material("BusRubber", new Color(0.025f, 0.025f, 0.03f), shader);
            var metal = Material("BusWheelHub", new Color(0.48f, 0.52f, 0.56f), shader);
            var amber = Material("BusAmber", new Color(1f, 0.65f, 0.08f), shader);
            var red = Material("BusRed", new Color(0.8f, 0.025f, 0.02f), shader);
            Transform root = visual.transform;
            Part(root, "White body", Vector3.zero, new Vector3(1f, 0.8f, 1f), white);
            Part(root, "Blue lower body", new Vector3(0f, -0.23f, 0f), new Vector3(1.006f, 0.33f, 1.006f), blue);
            Part(root, "White roof", new Vector3(0f, 0.44f, 0f), new Vector3(1f, 0.12f, 1f), white);
            Part(root, "Roof air conditioner", new Vector3(0f, 0.52f, 0.15f), new Vector3(0.65f, 0.08f, 0.3f), white);
            // Front faces the starting drone (local -Z).
            Part(root, "Windscreen", new Vector3(0f, 0.13f, -0.504f), new Vector3(0.9f, 0.36f, 0.012f), glass);
            Part(root, "Destination display", new Vector3(0f, 0.365f, -0.505f), new Vector3(0.9f, 0.11f, 0.012f), rubber);
            Part(root, "Front bumper", new Vector3(0f, -0.35f, -0.509f), new Vector3(0.96f, 0.08f, 0.018f), navy);
            Part(root, "Rear window", new Vector3(0f, 0.16f, 0.505f), new Vector3(0.86f, 0.3f, 0.012f), glass);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                    Part(root, "Side window", new Vector3(side * 0.504f, 0.16f, -0.37f + i * 0.145f), new Vector3(0.012f, 0.32f, 0.127f), glass);
                Part(root, "Navy waist stripe", new Vector3(side * 0.507f, -0.045f, 0f), new Vector3(0.015f, 0.04f, 0.99f), navy);
                Part(root, "Headlamp", new Vector3(side * 0.35f, -0.21f, -0.516f), new Vector3(0.18f, 0.065f, 0.018f), white);
                Part(root, "Indicator", new Vector3(side * 0.44f, -0.12f, -0.516f), new Vector3(0.06f, 0.035f, 0.018f), amber);
                Part(root, "Tail light", new Vector3(side * 0.4f, -0.2f, 0.512f), new Vector3(0.075f, 0.12f, 0.018f), red);
                Part(root, "Mirror arm", new Vector3(side * 0.56f, 0.22f, -0.44f), new Vector3(0.16f, 0.025f, 0.018f), navy);
                Part(root, "Mirror", new Vector3(side * 0.63f, 0.16f, -0.45f), new Vector3(0.06f, 0.16f, 0.025f), rubber);
                foreach (float z in new[] { -0.29f, 0.3f })
                {
                    Wheel(root, "Tyre", new Vector3(side * 0.49f, -0.32f, z), 0.065f, rubber);
                    Wheel(root, "Wheel hub", new Vector3(side * 0.528f, -0.32f, z), 0.018f, metal, 0.6f);
                }
            }
            // Left-side passenger doors for a bus facing -Z (Australian traffic).
            foreach (float z in new[] { -0.35f, 0.03f })
            {
                Part(root, "Passenger door frame", new Vector3(0.515f, -0.02f, z), new Vector3(0.018f, 0.68f, 0.14f), navy);
                Part(root, "Door glass", new Vector3(0.527f, 0.01f, z), new Vector3(0.008f, 0.57f, 0.115f), glass);
                Part(root, "Door centre rail", new Vector3(0.533f, 0.01f, z), new Vector3(0.008f, 0.59f, 0.006f), white);
            }
            Label(root, "CITY  431", new Vector3(0f, 0.365f, -0.518f), 0.07f, amber.color);
            Label(root, "PLEASE SIGNAL DRIVER", new Vector3(0f, -0.08f, -0.52f), 0.034f, Color.black);
            Part(root, "Signal driver sign", new Vector3(0f, -0.08f, -0.51f), new Vector3(0.65f, 0.06f, 0.012f), amber);
            Label(root, "SYDNEY", new Vector3(0f, -0.23f, -0.522f), 0.042f, Color.white);
            Undo.RegisterCreatedObjectUndo(visual, "Create Sydney bus visuals");
            UpdateRoadMarkings(bus, white);
        }

        private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Wheel(Transform parent, string name, Vector3 position, float width, Material material, float radiusScale = 1f)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = position;
            // Compensate for the bus's 2.5 x 3 x 8 scale to keep tyres circular.
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.36f * radiusScale, width, 0.135f * radiusScale);
            Object.DestroyImmediate(wheel.GetComponent<Collider>());
            wheel.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Label(Transform parent, string text, Vector3 position, float size, Color color)
        {
            var label = new GameObject(text);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localScale = new Vector3(1f, 2.5f / 3f, 1f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.color = color;
            // TextMesh fontSize affects mesh dimensions. Fit the actual generated
            // text bounds inside the sign rather than guessing a character size.
            Vector3 bounds = label.GetComponent<MeshRenderer>().localBounds.size;
            float maxWidth = text == "PLEASE SIGNAL DRIVER" ? 0.60f : 0.82f;
            const float heightCorrection = 2.5f / 3f;
            if (bounds.x > 0f && bounds.y > 0f)
            {
                float fit = Mathf.Min(maxWidth / bounds.x, size / (bounds.y * heightCorrection));
                label.transform.localScale = new Vector3(fit, fit * heightCorrection, fit);
            }
        }

        private static void UpdateRoadMarkings(GameObject bus, Material white)
        {
            Transform environment = bus.transform.parent;
            if (environment == null)
                return;
            Transform ground = environment.Find("Ground");
            if (ground == null || ground.GetComponent<Renderer>() == null)
                return;
            Transform previous = environment.Find("RoadMarkings");
            if (previous != null)
                Undo.DestroyObjectImmediate(previous.gameObject);
            var markings = new GameObject("RoadMarkings");
            markings.transform.SetParent(environment, false);
            Bounds road = ground.GetComponent<Renderer>().bounds;
            markings.transform.position = new Vector3(road.center.x, road.max.y + 0.015f, road.center.z);
            float halfLength = road.size.z * 0.5f;
            float halfWidth = road.size.x * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                Part(markings.transform, "Road edge", new Vector3(side * (halfWidth - 0.3f), 0f, 0f),
                    new Vector3(0.12f, 0.01f, road.size.z), white);
                for (float z = -halfLength + 1.5f; z < halfLength - 1.5f; z += 4f)
                    Part(markings.transform, "Lane dash", new Vector3(side * halfWidth / 3f, 0f, z),
                        new Vector3(0.12f, 0.01f, 2f), white);
            }
            Undo.RegisterCreatedObjectUndo(markings, "Create road markings");
        }

        private static Material Material(string name, Color color, Shader shader)
        {
            const string folder = "Assets/_Project/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            string path = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", name == "BusGlass" ? 0.7f : 0.25f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
