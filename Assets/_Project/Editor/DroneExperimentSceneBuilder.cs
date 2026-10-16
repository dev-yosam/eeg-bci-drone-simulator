using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EEGDroneExperiment.Editor
{
    public static class DroneExperimentSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/DroneBusCollision.unity";

        [MenuItem("Tools/EEG Drone/Build Experiment Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before building the experiment scene.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Rebuild experiment scene?",
                    "This replaces the saved DroneBusCollision scene. Any manual scene edits will be lost.",
                    "Rebuild", "Cancel"))
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader is unavailable. Check that the existing URP package has imported.");
                return;
            }

            EnsureFolder(Root);
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            var roadMaterial = GetMaterial("Road", new Color(0.16f, 0.18f, 0.2f), shader);
            var busMaterial = GetMaterial("Bus", new Color(1f, 0.65f, 0.04f), shader);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var manager = new GameObject("ExperimentManager").AddComponent<ExperimentManager>();
            var environment = new GameObject("Environment");
            CreateCube("Ground", environment.transform, new Vector3(0f, -0.1f, 15f),
                new Vector3(12f, 0.2f, 140f), roadMaterial);
            var bus = CreateCube("Bus", environment.transform, new Vector3(0f, 1.5f, 15f),
                new Vector3(2.5f, 3f, 8f), busMaterial);
            bus.AddComponent<CollisionEventMarker>();
            SydneyBusVisualBuilder.Build(bus);

            var drone = new GameObject("Drone");
            drone.transform.position = new Vector3(0f, 1.5f, 0f);
            drone.AddComponent<BoxCollider>().size = new Vector3(0.6f, 0.3f, 0.6f);
            var body = drone.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            drone.AddComponent<DroneController>().Configure(manager);
            manager.Configure(drone.GetComponent<DroneController>(), bus.GetComponent<BoxCollider>());
            TelloDroneVisualBuilder.Build(drone);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(drone.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 150f;
            camera.fieldOfView = 70f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.65f, 0.85f);
            cameraObject.AddComponent<AudioListener>();

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);

            SydneyIntersectionBuilder.Build(bus);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Could not save the experiment scene at " + ScenePath);
                return;
            }
            Selection.activeGameObject = drone;
            Debug.Log("Experiment scene saved: " + ScenePath + ". Press Play, click Game, press Enter, then hold W to approach the bus.");
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Material GetMaterial(string name, Color color, Shader shader)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
