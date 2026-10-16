using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EEGDroneExperiment
{
    // Short visual feedback only. Does not damage objects or generate event markers.
    public sealed class CollisionExplosion : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Light flash;
        private float age;

        public static void Play(Vector3 position)
        {
            var root = new GameObject("Bus Collision Explosion");
            root.transform.position = position;
            var effect = root.AddComponent<CollisionExplosion>();
            effect.Create();
            Destroy(root, 5f);
        }

        private void Create()
        {
            // Reuse Unity's built-in sphere mesh: no external textures or packages.
            var source = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            source.SetActive(false);
            Mesh sphere = source.GetComponent<MeshFilter>().sharedMesh;
            Destroy(source);

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("Explosion particle shader is unavailable; skipping particles.", this);
            }
            else
            {
                Emit("Fire", sphere, shader, new Color(1f, 0.35f, 0.025f, 1f),
                    32, 0.25f, 0.65f, 1.1f, 2.8f, 0.15f, 0.38f, 0f, false);
                Emit("Sparks", sphere, shader, new Color(1f, 0.8f, 0.22f, 1f),
                    42, 0.3f, 0.95f, 2f, 4.5f, 0.025f, 0.065f, 0.3f, false);
                Emit("Smoke", sphere, shader, new Color(0.2f, 0.22f, 0.24f, 0.6f),
                    16, 1.6f, 3.3f, 0.15f, 0.55f, 0.2f, 0.42f, 0f, true);
            }

            var lightObject = new GameObject("Brief warm flash");
            lightObject.transform.SetParent(transform, false);
            flash = lightObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.5f, 0.12f);
            flash.range = 5f;
            flash.intensity = 4f;
            flash.shadows = LightShadows.None;
        }

        private void Emit(string name, Mesh mesh, Shader shader, Color color, int count,
            float lifeMin, float lifeMax, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity, bool smoke)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(smoke ? Color.white : new Color(0.65f, 0.2f, 0.06f), 1f)
            }, new[] {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.65f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
            fade.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, smoke ? 0.6f : 1f, 1f, smoke ? 2.8f : 0.1f));
            if (smoke)
            {
                var velocity = particles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(0.65f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
            }

            var material = new Material(shader) { name = "Runtime " + name };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            materials.Add(material);
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            particles.Emit(count);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (flash != null)
            {
                flash.intensity = 4f * Mathf.Clamp01(1f - age / 0.18f);
                if (age >= 0.18f) flash.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materials)
                if (material != null) Destroy(material);
        }
    }
}
