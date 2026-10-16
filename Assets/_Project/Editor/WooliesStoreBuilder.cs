using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public static class WooliesStoreBuilder
    {
        [MenuItem("Tools/EEG Drone/Convert Selected Building to Woolies")]
        public static void ConvertSelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before updating the building.");
                return;
            }
            GameObject building = Selection.activeGameObject;
            if (building == null || !building.scene.IsValid() || building.GetComponent<MeshRenderer>() == null ||
                (building.name != "City block" && building.name != "Woolworths Supermarket"))
            {
                Debug.LogWarning("Select a City block under SydneyIntersection / City Buildings first.");
                return;
            }
            Build(building);
            EditorSceneManager.MarkSceneDirty(building.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Woolies storefront added. Save the scene with Ctrl+S.");
        }

        public static void Build(GameObject building)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("URP Lit shader is missing."); return; }
            Vector3 size = building.transform.localScale;
            if (size.x <= 0f || size.y <= 0f || size.z <= 0f) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Woolies storefront");
            var previous = building.transform.Find("Woolies Storefront");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            Undo.RecordObject(building, "Name supermarket");
            building.name = "Woolworths Supermarket";
            Material green = Material("WooliesGreen", new Color(0.025f, 0.32f, 0.10f), shader);
            Material lime = Material("WooliesLime", new Color(0.5f, 0.76f, 0.13f), shader);
            Material white = Material("WooliesWhite", new Color(0.94f, 0.95f, 0.90f), shader);
            Material glass = Material("WooliesGlass", new Color(0.06f, 0.18f, 0.19f), shader);
            Material dark = Material("WooliesFrame", new Color(0.12f, 0.15f, 0.13f), shader);
            var renderer = building.GetComponent<MeshRenderer>();
            Undo.RecordObject(renderer, "Supermarket wall colour");
            renderer.sharedMaterial = white;
            var root = new GameObject("Woolies Storefront");
            root.transform.SetParent(building.transform, false);
            bool facesPositiveZ = building.transform.localPosition.z < 0f;
            float direction = facesPositiveZ ? 1f : -1f;
            root.transform.localPosition = new Vector3(0f, -0.5f, direction * (0.5f + 0.04f / size.z));
            root.transform.localScale = new Vector3(1f / size.x, 1f / size.y, 1f / size.z);
            if (facesPositiveZ) root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform facade = root.transform;
            float width = size.x;
            Box(facade, "Green fascia", new Vector3(0f, 4f, -0.08f), new Vector3(width + 0.12f, 1.35f, 0.22f), green);
            Text(facade, "Woolworths", new Vector3(0.45f, 4.18f, -0.205f), width * 0.69f, 0.64f, Color.white);
            Text(facade, "The fresh food people", new Vector3(0.45f, 3.68f, -0.205f), width * 0.65f, 0.23f, Color.white);
            // A simple graphic badge, not an imported official logo asset.
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            badge.name = "Green badge";
            badge.transform.SetParent(facade, false);
            badge.transform.localPosition = new Vector3(-width * 0.38f, 4.05f, -0.21f);
            badge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            badge.transform.localScale = new Vector3(0.87f, 0.025f, 0.87f);
            badge.GetComponent<Renderer>().sharedMaterial = lime;
            Object.DestroyImmediate(badge.GetComponent<Collider>());
            Text(facade, "W", new Vector3(-width * 0.38f, 4.05f, -0.25f), 0.62f, 0.57f, Color.white);
            Box(facade, "Green awning", new Vector3(0f, 2.88f, -0.65f), new Vector3(width + 0.24f, 0.22f, 1.55f), green);
            Box(facade, "Lime awning trim", new Vector3(0f, 2.87f, -1.435f), new Vector3(width + 0.24f, 0.07f, 0.035f), lime);
            // Opaque glazing replaces the underlying solid block's storefront face.
            Box(facade, "Store glazing", new Vector3(0f, 1.45f, -0.075f), new Vector3(width - 0.35f, 2.5f, 0.08f), glass);
            for (int i = -3; i <= 3; i++)
                Box(facade, "Window mullion", new Vector3(i * (width - 0.5f) / 6f, 1.45f, -0.13f), new Vector3(0.065f, 2.5f, 0.065f), dark);
            Box(facade, "Entrance header", new Vector3(0f, 2.47f, -0.18f), new Vector3(2.3f, 0.35f, 0.1f), green);
            Text(facade, "Welcome", new Vector3(0f, 2.47f, -0.24f), 1.6f, 0.23f, Color.white);
            foreach (int side in new[] { -1, 1 })
            {
                Box(facade, "Sliding door frame", new Vector3(side * 1.1f, 1.2f, -0.18f), new Vector3(0.08f, 2.25f, 0.1f), white);
                Box(facade, "Door safety stripe", new Vector3(side * 0.53f, 1.12f, -0.2f), new Vector3(1f, 0.045f, 0.035f), white);
                Box(facade, "Fresh produce poster", new Vector3(side * width * 0.34f, 1.4f, -0.2f), new Vector3(1.05f, 1.25f, 0.05f), green);
                Text(facade, side == -1 ? "FRESH\nEVERY DAY" : "WOOLIES\nLOCAL", new Vector3(side * width * 0.34f, 1.4f, -0.24f), 0.9f, 0.85f, Color.white);
            }
            Undo.RegisterCreatedObjectUndo(root, "Create storefront");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = building;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
        }

        private static void Text(Transform parent, string value, Vector3 position, float width, float height, Color color)
        {
            var obj = new GameObject(value);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            var text = obj.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 64;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            Vector3 bounds = obj.GetComponent<MeshRenderer>().localBounds.size;
            if (bounds.x > 0f && bounds.y > 0f)
                obj.transform.localScale = Vector3.one * Mathf.Min(width / bounds.x, height / bounds.y);
        }

        private static Material Material(string name, Color color, Shader shader)
        {
            const string folder = "Assets/_Project/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            string path = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
