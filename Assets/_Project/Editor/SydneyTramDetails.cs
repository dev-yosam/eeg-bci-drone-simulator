using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EEGDroneExperiment.Editor
{
    public static class SydneyTramDetails
    {
        // Saved as an asset so the generated geometry survives closing the scene.
        public static void AddCab(Transform tram, bool rear, Material red, Material glass, Material silver)
        {
            const string folder = "Assets/_Project/Models";
            const string path = folder + "/SydneyTramCab.asset";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project", "Models");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = MakeCabMesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var cab = new GameObject(rear ? "Curved rear cab" : "Curved front cab");
            cab.transform.SetParent(tram, false);
            if (rear)
            {
                cab.transform.localPosition = new Vector3(26f, 0f, 0f);
                cab.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            cab.AddComponent<MeshFilter>().sharedMesh = mesh;
            cab.AddComponent<MeshRenderer>().sharedMaterials = new[] { red, glass, silver };
            cab.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Mesh MakeCabMesh()
        {
            // Each horizontal ring is a rounded nose, tapering backward at the roof.
            float[] heights = { 0.3f, 0.55f, 0.95f, 1.25f, 2.6f, 2.95f, 3.1f };
            float[] noses = { -0.45f, -0.85f, -1f, -0.85f, 0.15f, 0.55f, 0.9f };
            float[] widths = { 1.02f, 1.17f, 1.25f, 1.25f, 1.2f, 1.12f, 0.95f };
            const int segments = 20;
            const int ringSize = segments + 1;
            var vertices = new List<Vector3>();
            var faces = new[] { new List<int>(), new List<int>(), new List<int>() };
            for (int row = 0; row < heights.Length; row++)
                for (int column = 0; column <= segments; column++)
                {
                    float angle = -Mathf.PI / 2f + Mathf.PI * column / segments;
                    vertices.Add(new Vector3(1.45f - (1.45f - noses[row]) * Mathf.Cos(angle),
                        heights[row], widths[row] * Mathf.Sin(angle)));
                }
            for (int row = 0; row < heights.Length - 1; row++)
                for (int column = 0; column < ringSize; column++)
                {
                    int next = (column + 1) % ringSize;
                    int a = row * ringSize + column;
                    int b = row * ringSize + next;
                    int c = (row + 1) * ringSize + column;
                    int d = (row + 1) * ringSize + next;
                    bool windscreen = row == 3 || row == 4;
                    int material = windscreen ? ((column == 2 || column == 17) ? 2 : 1) : 0;
                    faces[material].AddRange(new[] { a, b, c, b, d, c });
                }
            // Close the top and bottom of the convex ring outline.
            for (int j = 1; j < segments; j++)
            {
                faces[0].AddRange(new[] { 0, j + 1, j });
                int top = (heights.Length - 1) * ringSize;
                faces[0].AddRange(new[] { top, top + j, top + j + 1 });
            }
            var mesh = new Mesh { name = "Sydney tram curved cab", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void AddStop(Transform parent, Material red, Material white, Material dark, Material paving)
        {
            var stop = new GameObject("Light Rail Stop - City Centre");
            stop.transform.SetParent(parent, false);
            // Beside the stopped tram, clear of the road and its swept width.
            stop.transform.localPosition = new Vector3(15f, 0f, -4.5f);
            Cube(stop.transform, "Platform", new Vector3(5f, 0.15f, 0f), new Vector3(22f, 0.3f, 2.4f), paving, true);
            Cube(stop.transform, "Platform edge", new Vector3(5f, 0.31f, 1.08f), new Vector3(22f, 0.025f, 0.12f), white);
            Cube(stop.transform, "Stop pylon", new Vector3(0f, 1.9f, 0f), new Vector3(0.22f, 3.8f, 0.22f), dark);
            Disc(stop.transform, "White roundel border", new Vector3(0f, 3.65f, 0f), 1.06f, white);
            Disc(stop.transform, "Red L roundel", new Vector3(0f, 3.65f, -0.065f), 0.93f, red);
            Disc(stop.transform, "Red L roundel back", new Vector3(0f, 3.65f, 0.065f), 0.93f, red);
            Cube(stop.transform, "Stop name panel", new Vector3(0f, 2.65f, 0f), new Vector3(2.3f, 0.8f, 0.15f), dark);
            foreach (bool back in new[] { false, true })
            {
                float sign = back ? 1f : -1f;
                Text(stop.transform, "L", new Vector3(0f, 3.65f, sign * 0.11f), back, 0.55f, 0.66f);
                Text(stop.transform, "CITY CENTRE", new Vector3(0f, 2.81f, sign * 0.085f), back, 2f, 0.22f);
                Text(stop.transform, "LIGHT RAIL  |  L2 / L3", new Vector3(0f, 2.48f, sign * 0.085f), back, 2f, 0.16f);
            }
            Cube(stop.transform, "Information board", new Vector3(2.4f, 1.45f, 0f), new Vector3(0.8f, 1.9f, 0.15f), dark);
            Text(stop.transform, "LIGHT RAIL\n\nL2  Randwick\nL3  Kingsford\n\nCity Centre\nDemo stop", new Vector3(2.4f, 1.55f, -0.09f), false, 0.68f, 1.5f);
        }

        private static void Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
        }

        private static void Disc(Transform parent, string name, Vector3 position, float diameter, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            obj.transform.localScale = new Vector3(diameter, 0.018f, diameter);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
        }

        private static void Text(Transform parent, string value, Vector3 position, bool back, float width, float height)
        {
            var obj = new GameObject(value);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            if (back) obj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = obj.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = 64;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            var bounds = obj.GetComponent<MeshRenderer>().localBounds.size;
            if (bounds.x > 0f && bounds.y > 0f)
                obj.transform.localScale = Vector3.one * Mathf.Min(width / bounds.x, height / bounds.y);
        }
    }
}
