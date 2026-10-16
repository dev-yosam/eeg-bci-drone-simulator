using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public static class TelloDroneVisualBuilder
    {
        [MenuItem("Tools/EEG Drone/Update Drone to Tello Style")]
        public static void UpdateDrone()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before updating the drone.");
                return;
            }
            int count = 0;
            foreach (var drone in Object.FindObjectsByType<DroneController>())
            {
                if (drone.gameObject.scene != EditorSceneManager.GetActiveScene()) continue;
                Build(drone.gameObject);
                count++;
            }
            if (count == 0)
            {
                Debug.LogWarning("Open DroneBusCollision or build the experiment scene first.");
                return;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Tello-style drone added. WASD: move; Space: ascend; Left Ctrl: descend. Save with Ctrl+S.");
        }

        public static void Build(GameObject drone)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader is missing.");
                return;
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Tello-style drone");
            Transform old = drone.transform.Find("TelloVisual");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var root = new GameObject("TelloVisual");
            root.transform.SetParent(drone.transform, false);
            // Keep the model behind the first-person camera, which is at local zero.
            root.transform.localPosition = new Vector3(0f, -0.005f, -0.15f);
            Material white = GetMaterial("TelloWhite", new Color(0.91f, 0.93f, 0.95f), shader);
            Material black = GetMaterial("TelloBlack", new Color(0.025f, 0.03f, 0.035f), shader);
            Material silver = GetMaterial("TelloMotor", new Color(0.46f, 0.5f, 0.53f), shader);
            Material lens = GetMaterial("TelloLens", new Color(0.025f, 0.10f, 0.15f), shader);
            Part(root.transform, "Lower body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.085f, 0.025f, 0.115f), black);
            Part(root.transform, "White top shell", PrimitiveType.Sphere, new Vector3(0f, 0.013f, 0f), new Vector3(0.096f, 0.028f, 0.126f), white);
            Part(root.transform, "Front camera housing", PrimitiveType.Cube, new Vector3(0f, 0f, 0.059f), new Vector3(0.033f, 0.023f, 0.014f), black);
            var cameraLens = Part(root.transform, "Front camera lens", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.067f), new Vector3(0.015f, 0.003f, 0.015f), lens);
            cameraLens.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (int xSign in new[] { -1, 1 })
                foreach (int zSign in new[] { -1, 1 })
                {
                    Vector3 motor = new Vector3(xSign * 0.09f, 0f, zSign * 0.077f);
                    Vector3 armStart = new Vector3(xSign * 0.027f, -0.006f, zSign * 0.033f);
                    Beam(root.transform, "Motor arm", armStart, motor, 0.011f, black);
                    Part(root.transform, "Motor", PrimitiveType.Cylinder, motor, new Vector3(0.021f, 0.014f, 0.021f), silver);
                    Part(root.transform, "Motor cap", PrimitiveType.Cylinder, motor + Vector3.up * 0.017f, new Vector3(0.015f, 0.005f, 0.015f), black);
                    var blade = Part(root.transform, "Two-blade propeller", PrimitiveType.Cube, motor + Vector3.up * 0.021f, new Vector3(0.085f, 0.002f, 0.009f), black);
                    blade.localRotation = Quaternion.Euler(0f, xSign * zSign * 30f, 0f);
                    Part(root.transform, "Landing foot", PrimitiveType.Cube, motor - Vector3.up * 0.023f, new Vector3(0.012f, 0.028f, 0.017f), black);
                    const int segments = 20;
                    const float radius = 0.048f;
                    for (int i = 0; i < segments; i++)
                    {
                        float a = i * 2f * Mathf.PI / segments;
                        float b = (i + 1) * 2f * Mathf.PI / segments;
                        Vector3 first = motor + new Vector3(Mathf.Cos(a) * radius, 0.011f, Mathf.Sin(a) * radius);
                        Vector3 second = motor + new Vector3(Mathf.Cos(b) * radius, 0.011f, Mathf.Sin(b) * radius);
                        Beam(root.transform, "Propeller guard", first, second, 0.003f, black);
                    }
                    Beam(root.transform, "Guard support", motor, motor + new Vector3(xSign * radius, 0.011f, 0f), 0.003f, black);
                }
            var body = drone.GetComponent<Rigidbody>();
            if (body != null)
            {
                Undo.RecordObject(body, "Enable vertical flight");
                body.useGravity = false;
                body.constraints = (body.constraints & ~RigidbodyConstraints.FreezePositionY) | RigidbodyConstraints.FreezeRotation;
            }
            // Keep the existing experimental collider dimensions and collision timing.
            Undo.RegisterCreatedObjectUndo(root, "Create drone model");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = drone;
        }

        private static Transform Part(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        private static void Beam(Transform parent, string name, Vector3 start, Vector3 end, float thickness, Material material)
        {
            Vector3 delta = end - start;
            Transform beam = Part(parent, name, PrimitiveType.Cube, (start + end) * 0.5f, new Vector3(thickness, thickness, delta.magnitude), material);
            beam.localRotation = Quaternion.LookRotation(delta);
        }

        private static Material GetMaterial(string name, Color color, Shader shader)
        {
            const string folder = "Assets/_Project/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            string path = folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
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
