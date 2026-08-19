using System.Collections.Generic;
using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Builds the 3D practice table out of Unity primitives: slate, rails, pocket
    /// mouths, cue ball and rack, cue stick, camera, lighting, and the control rig.
    ///
    /// Primitives are deliberate placeholders. Everything here is positioned from
    /// <see cref="PracticeTableLayout"/> in real metres, so swapping in real art later
    /// is a matter of replacing meshes and materials, not re-deriving the layout.
    /// </summary>
    public sealed class PracticeTableBuilder : MonoBehaviour
    {
        private static readonly Color[] BallColors =
        {
            new Color(0.95f, 0.78f, 0.20f), new Color(0.20f, 0.36f, 0.80f),
            new Color(0.83f, 0.24f, 0.22f), new Color(0.45f, 0.26f, 0.62f),
            new Color(0.92f, 0.49f, 0.16f), new Color(0.16f, 0.55f, 0.32f),
            new Color(0.55f, 0.20f, 0.22f), new Color(0.12f, 0.12f, 0.14f),
            new Color(0.97f, 0.85f, 0.45f), new Color(0.42f, 0.58f, 0.88f),
            new Color(0.88f, 0.45f, 0.42f), new Color(0.64f, 0.50f, 0.78f),
            new Color(0.95f, 0.68f, 0.42f), new Color(0.40f, 0.72f, 0.52f),
            new Color(0.72f, 0.42f, 0.44f)
        };

        [SerializeField, Min(0.001f)]
        private float physicsStepSeconds = 0.005f;

        private readonly List<Rigidbody> objectBalls = new List<Rigidbody>();
        private readonly List<PracticePocket> pockets = new List<PracticePocket>();

        private Material clothMaterial;
        private Material railMaterial;
        private Material pocketMaterial;
        private Material cueMaterial;
        private PhysicMaterial ballPhysicMaterial;
        private PhysicMaterial railPhysicMaterial;

        public Rigidbody CueBall { get; private set; }

        public IReadOnlyList<Rigidbody> ObjectBalls => objectBalls;

        public IReadOnlyList<PracticePocket> Pockets => pockets;

        public PracticeSessionController Session { get; private set; }

        private void Awake()
        {
            // Billiard balls are small and fast; a short fixed step keeps contacts
            // stable and stops hard shots tunnelling through a 5cm rail.
            Time.fixedDeltaTime = physicsStepSeconds;

            CreateMaterials();
            BuildLighting();
            BuildSlate();
            BuildRails();
            BuildPockets();
            BuildBalls();
            BuildRig();
        }

        private void CreateMaterials()
        {
            clothMaterial = CreateMaterial(new Color(0.09f, 0.42f, 0.28f));
            railMaterial = CreateMaterial(new Color(0.28f, 0.17f, 0.10f));
            pocketMaterial = CreateMaterial(new Color(0.03f, 0.03f, 0.04f));
            cueMaterial = CreateMaterial(new Color(0.78f, 0.63f, 0.42f));

            ballPhysicMaterial = new PhysicMaterial("PracticeBall")
            {
                dynamicFriction = 0.04f,
                staticFriction = 0.04f,
                bounciness = 0.94f,
                frictionCombine = PhysicMaterialCombine.Multiply,
                bounceCombine = PhysicMaterialCombine.Maximum
            };

            railPhysicMaterial = new PhysicMaterial("PracticeRail")
            {
                dynamicFriction = 0.2f,
                staticFriction = 0.2f,
                bounciness = 0.75f,
                frictionCombine = PhysicMaterialCombine.Average,
                bounceCombine = PhysicMaterialCombine.Maximum
            };
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.35f);
            }

            return material;
        }

        private void BuildLighting()
        {
            var lightObject = new GameObject("PracticeKeyLight");
            lightObject.transform.SetParent(transform, worldPositionStays: false);
            lightObject.transform.rotation = Quaternion.Euler(58f, 35f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);
        }

        private void BuildSlate()
        {
            const float slateThickness = 0.04f;

            var slate = CreatePrimitive(PrimitiveType.Cube, "Slate", clothMaterial);
            slate.transform.localScale = new Vector3(
                PracticeTableLayout.PlayfieldLengthMetres,
                slateThickness,
                PracticeTableLayout.PlayfieldWidthMetres);
            slate.transform.position = new Vector3(0f, -slateThickness * 0.5f, 0f);

            var slateCollider = slate.GetComponent<Collider>();
            slateCollider.material = new PhysicMaterial("PracticeCloth")
            {
                dynamicFriction = 0.2f,
                staticFriction = 0.2f,
                bounciness = 0.05f
            };
        }

        private void BuildRails()
        {
            var halfLength = PracticeTableLayout.HalfLength;
            var halfWidth = PracticeTableLayout.HalfWidth;
            var thickness = PracticeTableLayout.CushionThicknessMetres;
            var height = PracticeTableLayout.CushionHeightMetres;
            var corner = PracticeTableLayout.CornerPocketRadiusMetres;
            var side = PracticeTableLayout.SidePocketRadiusMetres;

            // Long rails are split at the side pockets; short rails span between corners.
            var longSpan = halfLength - corner - side;
            var longOffset = side + (longSpan * 0.5f);
            var shortSpan = (halfWidth * 2f) - (corner * 2f);

            AddRail("RailLongNearA", new Vector3(-longOffset, height * 0.5f, -halfWidth - (thickness * 0.5f)), new Vector3(longSpan, height, thickness));
            AddRail("RailLongNearB", new Vector3(longOffset, height * 0.5f, -halfWidth - (thickness * 0.5f)), new Vector3(longSpan, height, thickness));
            AddRail("RailLongFarA", new Vector3(-longOffset, height * 0.5f, halfWidth + (thickness * 0.5f)), new Vector3(longSpan, height, thickness));
            AddRail("RailLongFarB", new Vector3(longOffset, height * 0.5f, halfWidth + (thickness * 0.5f)), new Vector3(longSpan, height, thickness));
            AddRail("RailHead", new Vector3(-halfLength - (thickness * 0.5f), height * 0.5f, 0f), new Vector3(thickness, height, shortSpan));
            AddRail("RailFoot", new Vector3(halfLength + (thickness * 0.5f), height * 0.5f, 0f), new Vector3(thickness, height, shortSpan));
        }

        private void AddRail(string name, Vector3 position, Vector3 size)
        {
            var rail = CreatePrimitive(PrimitiveType.Cube, name, railMaterial);
            rail.transform.position = position;
            rail.transform.localScale = size;
            rail.GetComponent<Collider>().material = railPhysicMaterial;
        }

        private void BuildPockets()
        {
            var centres = PracticeTableLayout.PocketCentres();

            for (var i = 0; i < centres.Length; i++)
            {
                var centre = centres[i];
                var radius = PracticeTableLayout.PocketRadiusAt(centre);

                var visual = CreatePrimitive(PrimitiveType.Cylinder, $"PocketVisual{i}", pocketMaterial);
                Destroy(visual.GetComponent<Collider>());
                visual.transform.position = new Vector3(centre.x, 0.001f, centre.z);
                visual.transform.localScale = new Vector3(radius * 2f, 0.002f, radius * 2f);

                var trigger = new GameObject($"Pocket{i}");
                trigger.transform.SetParent(transform, worldPositionStays: false);
                trigger.transform.position = new Vector3(centre.x, -radius * 0.35f, centre.z);

                var collider = trigger.AddComponent<SphereCollider>();
                collider.radius = radius;
                collider.isTrigger = true;

                pockets.Add(trigger.AddComponent<PracticePocket>());
            }
        }

        private void BuildBalls()
        {
            CueBall = CreateBall("CueBall", PracticeTableLayout.CueBallSpot, Color.white);

            var rack = PracticeTableLayout.RackPositions(
                PracticeTableLayout.RackApex,
                PracticeTableLayout.BallRadiusMetres);

            for (var i = 0; i < rack.Length; i++)
            {
                objectBalls.Add(CreateBall($"Ball{i + 1}", rack[i], BallColors[i % BallColors.Length]));
            }
        }

        private Rigidbody CreateBall(string name, Vector3 position, Color color)
        {
            var radius = PracticeTableLayout.BallRadiusMetres;
            var ball = CreatePrimitive(PrimitiveType.Sphere, name, CreateMaterial(color));
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * (radius * 2f);

            var collider = ball.GetComponent<SphereCollider>();
            collider.material = ballPhysicMaterial;

            var body = ball.AddComponent<Rigidbody>();
            body.mass = PracticeTableLayout.BallMassKg;
            body.drag = 0.02f;
            body.angularDrag = 0.02f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 400f;

            ball.AddComponent<ClothContactMotion>().Configure(radius);
            return body;
        }

        private void BuildRig()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = false;
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50f;

            var orbit = gameObject.AddComponent<OrbitAimController>();
            orbit.Configure(camera, CueBall.transform);

            var cueStickObject = new GameObject("CueStickRig");
            cueStickObject.transform.SetParent(transform, worldPositionStays: false);
            var cueStick = cueStickObject.AddComponent<CueStickView>();
            cueStick.Configure(CueBall.transform, cueMaterial);

            var router = gameObject.AddComponent<PracticeInputRouter>();
            router.Configure(orbit);

            Session = gameObject.AddComponent<PracticeSessionController>();
            Session.Configure(CueBall, objectBalls, pockets, router, orbit, cueStick);

            gameObject.AddComponent<PracticeHud>().Configure(Session, router, orbit);
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Material material)
        {
            var created = GameObject.CreatePrimitive(type);
            created.name = name;
            created.transform.SetParent(transform, worldPositionStays: false);
            created.GetComponent<Renderer>().sharedMaterial = material;
            return created;
        }
    }
}
