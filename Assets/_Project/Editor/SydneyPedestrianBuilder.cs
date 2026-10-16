using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public static class SydneyPedestrianBuilder
    {
        [MenuItem("Tools/EEG Drone/Add Pedestrians")]
        public static void AddToScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before adding pedestrians.");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.name != "SydneyIntersection") continue;
                Build(obj.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Pedestrians added to pavements and the tram stop. Save with Ctrl+S.");
                return;
            }
            Debug.LogWarning("Add the Sydney intersection first, then run Add Pedestrians.");
        }

        public static void Build(Transform intersection)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { Debug.LogError("URP Lit shader is missing."); return; }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add pedestrians");
            Transform previous = intersection.Find("Pedestrians");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var crowd = new GameObject("Pedestrians");
            crowd.transform.SetParent(intersection, false);
            Material[] shirts = {
                GetMaterial("PedestrianTeal", new Color(0.08f, 0.45f, 0.45f), shader),
                GetMaterial("PedestrianOchre", new Color(0.85f, 0.52f, 0.12f), shader),
                GetMaterial("PedestrianBurgundy", new Color(0.48f, 0.08f, 0.16f), shader),
                GetMaterial("PedestrianBlue", new Color(0.15f, 0.28f, 0.65f), shader),
                GetMaterial("PedestrianCream", new Color(0.85f, 0.83f, 0.74f), shader)
            };
            Material[] skins = {
                GetMaterial("PedestrianSkinLight", new Color(0.85f, 0.63f, 0.46f), shader),
                GetMaterial("PedestrianSkinMedium", new Color(0.57f, 0.35f, 0.22f), shader),
                GetMaterial("PedestrianSkinDark", new Color(0.28f, 0.16f, 0.11f), shader)
            };
            Material trousers = GetMaterial("PedestrianTrousers", new Color(0.12f, 0.16f, 0.21f), shader);
            Material dark = GetMaterial("PedestrianShoesHair", new Color(0.035f, 0.03f, 0.025f), shader);
            Material bag = GetMaterial("PedestrianBag", new Color(0.32f, 0.4f, 0.2f), shader);
            // Local to the junction; feet are on the 0.18 m pavement or 0.30 m platform.
            Vector3[] positions = {
                new Vector3(-7.7f, 0.18f, -7f), new Vector3(-7.5f, 0.18f, -9f),
                new Vector3(7.7f, 0.18f, -7.5f), new Vector3(7.5f, 0.18f, 8f),
                new Vector3(-7.8f, 0.18f, 8f),
                new Vector3(-12.5f, 0.18f, 4.7f), new Vector3(-14f, 0.18f, 5f),
                new Vector3(-17f, 0.18f, 4.7f), new Vector3(-18.3f, 0.18f, 5f),
                new Vector3(-14f, 0.18f, -4.5f),
                new Vector3(13f, 0.30f, -4.5f), new Vector3(16f, 0.30f, -4.3f),
                new Vector3(19f, 0.30f, -4.6f), new Vector3(22f, 0.30f, -4.3f),
                new Vector3(16f, 0.18f, 4.8f), new Vector3(19f, 0.18f, 4.6f)
            };
            float[] headings = { 10f, 170f, -35f, 180f, 140f, 15f, -20f, 70f, -70f, 25f, 0f, 0f, 15f, -10f, 180f, 160f };
            for (int i = 0; i < positions.Length; i++)
            {
                var person = new GameObject("Pedestrian " + (i + 1).ToString("00"));
                person.transform.SetParent(crowd.transform, false);
                person.transform.localPosition = positions[i];
                person.transform.localRotation = Quaternion.Euler(0f, headings[i], 0f);
                person.transform.localScale = Vector3.one * (0.92f + (i % 4) * 0.04f);
                CreatePerson(person.transform, shirts[i % shirts.Length], skins[i % skins.Length], trousers, dark, bag, i % 3);
            }
            Undo.RegisterCreatedObjectUndo(crowd, "Create pedestrians");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = crowd;
        }

        private static void CreatePerson(Transform parent, Material shirt, Material skin, Material trousers, Material dark, Material bag, int pose)
        {
            Part(parent, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.36f, 0.30f, 0.25f), shirt);
            Part(parent, "Hips", PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0f), new Vector3(0.32f, 0.22f, 0.23f), trousers);
            Part(parent, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.11f, 0.065f, 0.11f), skin);
            Part(parent, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 0f), new Vector3(0.24f, 0.30f, 0.25f), skin);
            Part(parent, "Hair", PrimitiveType.Sphere, new Vector3(0f, 1.745f, -0.018f), new Vector3(0.25f, 0.13f, 0.25f), dark);
            Part(parent, "Nose", PrimitiveType.Sphere, new Vector3(0f, 1.64f, 0.125f), new Vector3(0.045f, 0.065f, 0.055f), skin);
            foreach (int side in new[] { -1, 1 })
            {
                float footZ = pose == 1 ? side * 0.065f : 0f;
                Limb(parent, "Leg", new Vector3(side * 0.095f, 0.86f, 0f), new Vector3(side * 0.12f, 0.12f, footZ), 0.13f, trousers);
                Part(parent, "Shoe", PrimitiveType.Cube, new Vector3(side * 0.12f, 0.065f, footZ + 0.04f), new Vector3(0.14f, 0.13f, 0.25f), dark);
                Vector3 shoulder = new Vector3(side * 0.2f, 1.34f, 0f);
                Vector3 elbow = new Vector3(side * 0.25f, 1.08f, 0.01f);
                Vector3 hand = new Vector3(side * 0.25f, 0.83f, 0.05f);
                if (pose == 1)
                {
                    elbow = new Vector3(side * 0.24f, 1.08f, 0.08f);
                    hand = new Vector3(side * 0.06f, 1.19f, 0.28f);
                }
                Limb(parent, "Sleeve", shoulder, elbow, 0.12f, shirt);
                Limb(parent, "Forearm", elbow, hand, 0.085f, skin);
                Part(parent, "Hand", PrimitiveType.Sphere, hand, new Vector3(0.09f, 0.12f, 0.08f), skin);
            }
            if (pose == 0)
            {
                Part(parent, "Shopping bag", PrimitiveType.Cube, new Vector3(0.29f, 0.58f, 0.04f), new Vector3(0.22f, 0.3f, 0.14f), bag);
                Limb(parent, "Bag handle", new Vector3(0.25f, 0.82f, 0.05f), new Vector3(0.29f, 0.72f, 0.04f), 0.025f, bag);
            }
            else if (pose == 1)
                Part(parent, "Phone", PrimitiveType.Cube, new Vector3(0f, 1.21f, 0.3f), new Vector3(0.09f, 0.15f, 0.02f), dark);
            else
                Part(parent, "Backpack", PrimitiveType.Capsule, new Vector3(0f, 1.18f, -0.18f), new Vector3(0.27f, 0.23f, 0.18f), bag);
        }

        private static void Limb(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material)
        {
            Vector3 delta = end - start;
            Transform limb = Part(parent, name, PrimitiveType.Capsule, (start + end) / 2f, new Vector3(width, delta.magnitude / 2f, width), material);
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
        }

        private static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            // Scenery only; people do not block the drone or generate experimental events.
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        private static Material GetMaterial(string name, Color color, Shader shader)
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
