using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public static class SydneyStreetscapeBuilder
    {
        [MenuItem("Tools/EEG Drone/Update Crosswalks and Roadside Buildings")]
        public static void UpdateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before updating streets.");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.name != "SydneyIntersection") continue;
                Build(obj.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Full-width zebra crossings and low-rise street blocks updated. Save with Ctrl+S.");
                return;
            }
            Debug.LogWarning("Open the experiment scene and add the Sydney intersection first.");
        }

        public static void Build(Transform intersection)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("URP Lit shader is missing."); return; }
            Transform streets = intersection.Find("Streets and Crossings");
            Transform buildings = intersection.Find("City Buildings");
            if (streets == null || buildings == null) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Update crosswalks and buildings");
            // Replace only generated crossings and extensions; retain the original shops.
            for (int i = streets.childCount - 1; i >= 0; i--)
            {
                Transform child = streets.GetChild(i);
                if (child.name == "Pedestrian crossing" || child.name == "Zebra Crossings")
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
            Transform oldBlocks = buildings.Find("Extended Low Rise Blocks");
            if (oldBlocks != null) Undo.DestroyObjectImmediate(oldBlocks.gameObject);
            Transform oldPaving = streets.Find("Extended Approach Pavements");
            if (oldPaving != null) Undo.DestroyObjectImmediate(oldPaving.gameObject);
            Material white = Material("StreetMarkingWhite", new Color(0.94f, 0.94f, 0.90f), shader);
            Material glass = Material("StreetShopGlass", new Color(0.055f, 0.14f, 0.18f), shader);
            Material trim = Material("StreetShopTrim", new Color(0.14f, 0.19f, 0.20f), shader);
            Material paving = Material("CityPaving", new Color(0.63f, 0.59f, 0.53f), shader);
            Material[] walls = {
                Material("StreetSandstone", new Color(0.7f, 0.6f, 0.45f), shader),
                Material("StreetBrick", new Color(0.51f, 0.28f, 0.21f), shader),
                Material("StreetCream", new Color(0.78f, 0.76f, 0.65f), shader),
                Material("StreetGrey", new Color(0.48f, 0.54f, 0.55f), shader)
            };
            Transform crossings = Group(streets, "Zebra Crossings");
            foreach (int side in new[] { -1, 1 })
            {
                // 12 m approach road: stripes reach both pavement edges.
                for (int stripe = 0; stripe < 16; stripe++)
                    Box(crossings, "Approach zebra stripe", new Vector3(-5.7f + stripe * 0.76f, 0.088f, side * 2.25f),
                        new Vector3(0.48f, 0.012f, 1.5f), white);
                // 6 m cross street: cross both light-rail tracks.
                for (int stripe = 0; stripe < 8; stripe++)
                    Box(crossings, "Tram street zebra stripe", new Vector3(side * 7.25f, 0.088f, -2.66f + stripe * 0.76f),
                        new Vector3(1.5f, 0.012f, 0.48f), white);
            }
            Transform blocks = Group(buildings, "Extended Low Rise Blocks");
            int index = 0;
            foreach (int side in new[] { -1, 1 })
            {
                // Continue the cross-street rows beyond the existing three corner buildings.
                foreach (int end in new[] { -1, 1 })
                    for (int i = 0; i < 3; i++)
                        Building(blocks, new Vector3(side * (45f + i * 10f), 0f, end * 12f),
                            end == 1 ? 0f : 180f, index++, walls, glass, trim);
                // Approach road extends from local z=-62 to +78 in the default scene.
                // Existing corner blocks occupy z=+/-12; keep new rows beyond them.
                foreach (float z in new[] { -52f, -42f, -32f, -22f, 22f, 32f, 42f, 52f, 62f, 72f })
                    Building(blocks, new Vector3(side * 15f, 0f, z), side == 1 ? 90f : -90f,
                        index++, walls, glass, trim);
            }
            // Fill the pavement between the original endpoint and the extended north row.
            Transform pavement = Group(streets, "Extended Approach Pavements");
            foreach (int side in new[] { -1, 1 })
                Box(pavement, "North pavement extension", new Vector3(side * 7.7f, 0.06f, 68f),
                    new Vector3(3f, 0.24f, 20f), paving, true);
            Undo.RegisterCreatedObjectUndo(crossings.gameObject, "Create zebra crossings");
            Undo.RegisterCreatedObjectUndo(blocks.gameObject, "Create street blocks");
            Undo.RegisterCreatedObjectUndo(pavement.gameObject, "Extend pavement");
            Undo.CollapseUndoOperations(group);
        }

        private static void Building(Transform parent, Vector3 position, float yaw, int index, Material[] walls, Material glass, Material trim)
        {
            Transform building = Group(parent, "Low Rise Shop " + (index + 1));
            building.localPosition = position;
            building.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float height = 6f + index % 3;
            Box(building, "Building", new Vector3(0f, height / 2f, 0f), new Vector3(9.5f, height, 10f), walls[index % walls.Length], true);
            Box(building, "Roof trim", new Vector3(0f, height, 0f), new Vector3(9.7f, 0.18f, 10.2f), trim);
            Box(building, "Shop windows", new Vector3(0f, 1.5f, -5.025f), new Vector3(8.6f, 2.4f, 0.04f), glass);
            for (int column = -2; column <= 2; column++)
            {
                Box(building, "Window frame", new Vector3(column * 1.75f, 1.5f, -5.06f), new Vector3(0.08f, 2.4f, 0.05f), trim);
                Box(building, "Upper window", new Vector3(column * 1.75f, 4.5f, -5.025f), new Vector3(1.05f, 1.4f, 0.04f), glass);
            }
            Box(building, "Awning", new Vector3(0f, 2.9f, -5.65f), new Vector3(9.6f, 0.16f, 1.3f), trim);
        }

        private static Transform Group(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
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
