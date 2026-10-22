using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RayOEZoning.Editor
{
    /// <summary>
    /// Locally authored props. Each prop faces local +z with its footprint centred on the origin; wall-mounted props
    /// take the wall surface point as their origin and sit entirely in front of it.
    /// </summary>
    public static partial class ZoningBuilder
    {
        static Material extinguisherRed;
        static System.Random dressing;
        static readonly List<ClueSpot> spots = new List<ClueSpot>();
        static readonly Vector3 AlongZ = new Vector3(90, 0, 0);

        static Transform Placed(Transform parent, string name, Vector3 at, float yaw)
        {
            var t = Child(parent, name); t.localPosition = at; t.localRotation = Quaternion.Euler(0, yaw, 0); return t;
        }

        static GameObject Box(Transform parent, string name, Vector3 at, Vector3 size, Material material) =>
            Primitive(parent, name, PrimitiveType.Cube, at, size, material, false);

        static GameObject Cylinder(Transform parent, string name, Vector3 at, float radius, float height, Material material, Vector3 euler = default)
        {
            var go = Primitive(parent, name, PrimitiveType.Cylinder, at, new Vector3(radius * 2, height * .5f, radius * 2), material, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            return go;
        }

        static GameObject GlassPane(Transform parent, Vector3 at, Vector3 size)
        {
            var go = Primitive(parent, "Glass", PrimitiveType.Cube, at, size, glass, true);
            go.layer = GlassLayer;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------ furniture
        static void LockerBank(Transform parent, Vector3 at, float yaw, int count)
        {
            var bank = Placed(parent, "Lockers", at, yaw);
            float width = count * .42f;
            Solid(bank, null, new Vector3(0, 1.05f, 0), new Vector3(width, 1.9f, .5f), lockerPaint);
            Box(bank, null, new Vector3(0, .05f, -.02f), new Vector3(width, .1f, .46f), skirting);
            Box(bank, null, new Vector3(0, 2.02f, 0), new Vector3(width + .02f, .04f, .52f), lockerPaint);
            for (int i = 0; i < count; i++)
            {
                var door = Placed(bank, "Locker", new Vector3(-width * .5f + .21f + i * .42f, 0, 0), 0);
                Box(door, null, new Vector3(0, 1.06f, .255f), new Vector3(.40f, 1.82f, .018f), i % 3 == 1 ? enamelTeal : lockerPaint);
                for (int k = 0; k < 4; k++) Box(door, null, new Vector3(0, 1.68f + k * .045f, .266f), new Vector3(.22f, .016f, .006f), darkMetal);
                Box(door, null, new Vector3(.13f, 1.02f, .272f), new Vector3(.024f, .14f, .03f), stainless);
                Box(door, null, new Vector3(0, 1.5f, .266f), new Vector3(.11f, .045f, .005f), paper);
            }
        }

        static void ChangingBench(Transform parent, Vector3 at, float yaw, float length)
        {
            var bench = Placed(parent, "Bench", at, yaw);
            Solid(bench, null, new Vector3(0, .43f, 0), new Vector3(length, .05f, .36f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(bench, null, new Vector3(side * (length * .5f - .12f), .21f, 0), new Vector3(.05f, .42f, .32f), stainless);
                Box(bench, null, new Vector3(side * (length * .5f - .12f), .015f, 0), new Vector3(.08f, .03f, .36f), darkMetal);
            }
        }

        static void SecurityDesk(Transform parent, Vector3 at, float yaw, float width)
        {
            var desk = Placed(parent, "Desk", at, yaw);
            Solid(desk, null, new Vector3(0, .53f, .1f), new Vector3(width, 1.06f, .5f), enamel);
            Box(desk, null, new Vector3(0, .62f, .355f), new Vector3(width, .08f, .012f), accentTeal);
            Box(desk, null, new Vector3(0, 1.08f, .14f), new Vector3(width + .04f, .04f, .6f), epoxy);
            Solid(desk, null, new Vector3(0, .37f, -.35f), new Vector3(width - .2f, .74f, .62f), plasticGrey);
            Box(desk, null, new Vector3(0, .755f, -.35f), new Vector3(width - .16f, .03f, .66f), wood);
            Material[] feeds = { screenFeed, screenGraph, screenFeed };
            for (int i = 0; i < 3; i++)
                Monitor(desk, new Vector3((i - 1) * .62f, .77f, -.3f), 180 + (i - 1) * 14, feeds[i]);
            Box(desk, "Keyboard", new Vector3(0, .78f, -.52f), new Vector3(.42f, .02f, .14f), darkMetal);
            Mug(desk, new Vector3(width * .5f - .25f, .77f, -.5f));
            var chair = Placed(desk, "Chair", new Vector3(.15f, 0, -1.05f), 160);
            Cylinder(chair, null, new Vector3(0, .24f, 0), .03f, .4f, stainless);
            Box(chair, null, new Vector3(0, .48f, 0), new Vector3(.48f, .07f, .46f), rubber);
            Box(chair, null, new Vector3(0, .82f, -.22f), new Vector3(.44f, .52f, .06f), rubber);
            Box(chair, null, new Vector3(0, .58f, -.2f), new Vector3(.05f, .2f, .03f), stainless);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72;
                Box(chair, null, Quaternion.Euler(0, a, 0) * new Vector3(0, .05f, .18f), new Vector3(.04f, .03f, .34f), darkMetal).transform.localRotation = Quaternion.Euler(0, a, 0);
                Cylinder(chair, null, Quaternion.Euler(0, a, 0) * new Vector3(0, .02f, .34f), .02f, .04f, rubber, new Vector3(0, 0, 90));
            }
        }

        static void Shelving(Transform parent, Vector3 at, float yaw, float width, float height)
        {
            var shelf = Placed(parent, "Shelf", at, yaw);
            Solid(shelf, null, new Vector3(0, height * .5f, 0), new Vector3(width - .06f, height, .05f), enamel);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box(shelf, null, new Vector3(x * (width * .5f - .025f), height * .5f, z * .23f), new Vector3(.04f, height, .04f), steel);
            Material[] binders = { binderBlue, binderRed, binderYellow, paper, plasticGrey };
            for (int level = 0; level < 5; level++)
            {
                float y = .12f + level * (height - .3f) / 4;
                Box(shelf, null, new Vector3(0, y, 0), new Vector3(width, .025f, .5f), steel);
                if (level == 4) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = -width * .5f + .12f;
                    while (x < width * .5f - .2f)
                    {
                        bool box = dressing.NextDouble() < .35;
                        float w = box ? .28f + (float)dressing.NextDouble() * .12f : .06f + (float)dressing.NextDouble() * .03f;
                        float h = box ? .18f + (float)dressing.NextDouble() * .1f : .27f + (float)dressing.NextDouble() * .05f;
                        if (dressing.NextDouble() < .15) { x += .18f; continue; }
                        Box(shelf, box ? "Box" : "Binder", new Vector3(x + w * .5f, y + .0125f + h * .5f, side * .13f), new Vector3(w, h, box ? .3f : .22f),
                            box ? (dressing.NextDouble() < .5 ? paper : plasticGrey) : binders[dressing.Next(binders.Length)]);
                        x += w + .015f;
                    }
                }
            }
        }

        /// <summary>A long island bench. <paramref name="reserve"/> keeps a slot free on the -z face for a clue screen.</summary>
        static Transform LabBench(Transform parent, Vector3 at, float yaw, float length, int seed, float reserve = float.NaN)
        {
            var random = new System.Random(seed);
            var bench = Placed(parent, "Lab Bench", at, yaw);
            const float depth = 1.5f;
            Solid(bench, null, new Vector3(0, .45f, 0), new Vector3(length, .9f, depth), enamel);
            Box(bench, null, new Vector3(0, .05f, 0), new Vector3(length - .04f, .1f, depth - .08f), skirting);
            Box(bench, null, new Vector3(0, .92f, 0), new Vector3(length + .04f, .04f, depth + .04f), epoxy);
            Solid(bench, null, new Vector3(0, 1.24f, 0), new Vector3(length, .64f, .22f), enamelTeal);
            Box(bench, null, new Vector3(0, 1.57f, 0), new Vector3(length + .02f, .02f, .24f), stainless);
            int doors = Mathf.Max(2, Mathf.RoundToInt(length / .6f));
            for (int side = -1; side <= 1; side += 2)
            {
                float face = side * (depth * .5f + .004f);
                for (int i = 0; i <= doors; i++)
                {
                    float x = -length * .5f + i * length / doors;
                    if (i > 0 && i < doors) Box(bench, null, new Vector3(x, .5f, face), new Vector3(.008f, .76f, .006f), darkMetal);
                    if (i < doors) Box(bench, null, new Vector3(x + length / doors * .5f, .78f, face + side * .012f), new Vector3(.16f, .014f, .022f), stainless);
                }
                for (int i = 0; i < Mathf.Max(1, (int)(length / 1.1f)); i++)
                    Box(bench, null, new Vector3(-length * .5f + .5f + i * 1.1f, 1.2f, side * .112f), new Vector3(.14f, .08f, .006f), paper);
            }
            Box(bench, null, new Vector3(0, 1.82f, 0), new Vector3(length - .1f, .02f, .32f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 3; i++)
                Box(bench, null, new Vector3(-length * .45f + i * length * .45f, 1.7f, 0), new Vector3(.03f, .24f, .3f), stainless);
            for (int i = 0; i < (int)(length / .22f); i++)
            {
                if (random.NextDouble() < .35) continue;
                float h = .1f + (float)random.NextDouble() * .12f;
                float x = -length * .5f + .15f + i * .22f;
                Cylinder(bench, "Bottle", new Vector3(x, 1.83f + h * .5f, (float)random.NextDouble() * .16f - .08f), .026f + (float)random.NextDouble() * .012f, h * .8f, random.NextDouble() < .55 ? amberGlass : glass);
            }
            // Instruments on both work faces, standing on the worktop (top at 0.94 m).
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * .45f;
                float x = -length * .5f + .45f;
                while (x < length * .5f - .4f)
                {
                    if (side < 0 && !float.IsNaN(reserve) && x > reserve - 1.0f && x < reserve + .5f) { x = reserve + .55f; continue; }
                    int kind = random.Next(5);
                    if (kind == 0) { Microscope(bench, new Vector3(x, .94f, z), side > 0 ? 0 : 180); x += .7f; }
                    else if (kind == 1) { Monitor(bench, new Vector3(x, .94f, z - side * .05f), side > 0 ? 0 : 180, random.NextDouble() < .5 ? screenGraph : screenMolecule); x += .8f; }
                    else if (kind == 2) { Centrifuge(bench, new Vector3(x, .94f, z), side > 0 ? 0 : 180); x += .65f; }
                    else if (kind == 3) { Glassware(bench, new Vector3(x, .94f, z), side > 0 ? 0 : 180, random); x += .55f; }
                    else { x += .45f; }
                }
            }
            return bench;
        }

        static void Microscope(Transform parent, Vector3 at, float yaw)
        {
            var item = Placed(parent, "Microscope", at, yaw);
            Box(item, null, new Vector3(0, .02f, 0), new Vector3(.2f, .04f, .26f), enamel);
            Box(item, null, new Vector3(0, .17f, -.08f), new Vector3(.06f, .3f, .07f), enamel);
            Box(item, null, new Vector3(0, .14f, .03f), new Vector3(.16f, .015f, .14f), darkMetal);
            Box(item, null, new Vector3(0, .14f, -.03f), new Vector3(.04f, .02f, .06f), enamel);
            Cylinder(item, null, new Vector3(0, .25f, .02f), .045f, .05f, darkMetal);
            Box(item, null, new Vector3(0, .29f, -.04f), new Vector3(.05f, .04f, .1f), enamel);
            Cylinder(item, null, new Vector3(0, .34f, -.03f), .02f, .16f, darkMetal, new Vector3(-35, 0, 0));
        }

        /// <summary>Desk monitor: foot, neck and back plate connect the screen to the surface it stands on.</summary>
        static Transform Monitor(Transform parent, Vector3 at, float yaw, Material screen)
        {
            var monitor = Placed(parent, "Monitor", at, yaw);
            Box(monitor, null, new Vector3(0, .01f, 0), new Vector3(.24f, .02f, .17f), darkMetal);
            Box(monitor, null, new Vector3(0, .17f, -.05f), new Vector3(.045f, .3f, .03f), darkMetal);
            Box(monitor, null, new Vector3(0, .36f, -.04f), new Vector3(.1f, .1f, .02f), darkMetal);
            Box(monitor, null, new Vector3(0, .36f, -.015f), new Vector3(.54f, .33f, .03f), darkMetal);
            Box(monitor, null, new Vector3(0, .36f, .001f), new Vector3(.5f, .29f, .002f), screen);
            return monitor;
        }

        /// <summary>Screen hung on a rail or wall. Its back plate is at the origin.</summary>
        static Transform WallMonitor(Transform parent, Vector3 at, float yaw, Material screen)
        {
            var monitor = Placed(parent, "Monitor", at, yaw);
            Box(monitor, null, new Vector3(0, 0, .01f), new Vector3(.1f, .1f, .02f), darkMetal);
            Box(monitor, null, new Vector3(0, 0, .035f), new Vector3(.54f, .33f, .03f), darkMetal);
            Box(monitor, null, new Vector3(0, 0, .051f), new Vector3(.5f, .29f, .002f), screen);
            return monitor;
        }

        static void Centrifuge(Transform parent, Vector3 at, float yaw)
        {
            var item = Placed(parent, "Centrifuge", at, yaw);
            Box(item, null, new Vector3(0, .12f, 0), new Vector3(.42f, .24f, .42f), enamel);
            Cylinder(item, null, new Vector3(0, .245f, -.02f), .17f, .02f, plasticGrey);
            Box(item, null, new Vector3(0, .12f, .212f), new Vector3(.24f, .08f, .004f), darkMetal);
            Box(item, null, new Vector3(.08f, .12f, .215f), new Vector3(.03f, .02f, .004f), ledGreen);
        }

        static void Glassware(Transform parent, Vector3 at, float yaw, System.Random random)
        {
            var item = Placed(parent, "Test Tubes", at, yaw);
            Box(item, null, new Vector3(0, .04f, 0), new Vector3(.3f, .08f, .1f), plasticGrey);
            for (int i = 0; i < 6; i++) Cylinder(item, null, new Vector3(-.12f + i * .048f, .1f, 0), .012f, .12f, i % 2 == 0 ? glass : binderBlue);
            Cylinder(item, null, new Vector3(.24f, .07f, .05f), .05f, .14f, glass);
            Cylinder(item, null, new Vector3(-.26f, .05f, .06f), .06f, .1f, glass);
        }

        static void Mug(Transform parent, Vector3 at)
        {
            var mug = Placed(parent, "Mug", at, 30);
            Cylinder(mug, null, new Vector3(0, .05f, 0), .04f, .1f, enamel);
            Cylinder(mug, null, new Vector3(0, .085f, 0), .034f, .002f, epoxy);
            Box(mug, null, new Vector3(.048f, .05f, 0), new Vector3(.016f, .06f, .012f), enamel);
        }

        static void FumeHood(Transform parent, Vector3 at, float yaw, float width)
        {
            var hood = Placed(parent, "Fume Hood", at, yaw);
            const float depth = .85f, height = 2.35f;
            Solid(hood, null, new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), enamel, false);
            Box(hood, null, new Vector3(0, .43f, 0), new Vector3(width, .86f, depth), enamel);
            Box(hood, null, new Vector3(0, .89f, .02f), new Vector3(width + .01f, .04f, depth - .02f), epoxy);
            for (int side = -1; side <= 1; side += 2)
                Box(hood, null, new Vector3(side * (width * .5f - .04f), 1.5f, 0), new Vector3(.08f, 1.2f, depth), enamel);
            Box(hood, null, new Vector3(0, 1.5f, -depth * .5f + .05f), new Vector3(width - .1f, 1.2f, .03f), stainless);
            Box(hood, null, new Vector3(0, 2.2f, 0), new Vector3(width, .3f, depth), enamel);
            Box(hood, null, new Vector3(0, 2.04f, .05f), new Vector3(width - .25f, .02f, .2f), nightPanel);
            Box(hood, null, new Vector3(0, 1.62f, depth * .5f - .03f), new Vector3(width - .14f, .76f, .012f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(hood, null, new Vector3(0, 1.24f, depth * .5f), new Vector3(width - .2f, .03f, .04f), stainless);
            Box(hood, null, new Vector3(width * .5f - .2f, 2.2f, depth * .5f + .002f), new Vector3(.16f, .08f, .006f), darkMetal);
            Box(hood, null, new Vector3(width * .5f - .2f, 2.2f, depth * .5f + .006f), new Vector3(.1f, .02f, .004f), ledGreen);
            Cylinder(hood, null, new Vector3(0, (height + Ceiling) * .5f, -.1f), .15f, Ceiling - height, steel);
            for (int i = 0; i < 2; i++) Glassware(hood, new Vector3(-.3f + i * .55f, .91f, -.05f), 0, dressing);
        }

        static void InstrumentCabinet(Transform parent, Vector3 at, float yaw, float width, float height)
        {
            var cabinet = Placed(parent, "Cabinet", at, yaw);
            const float depth = .55f;
            Solid(cabinet, null, new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), enamel);
            Box(cabinet, null, new Vector3(0, height * .58f, depth * .5f + .003f), new Vector3(width - .1f, height * .62f, .004f), darkMetal);
            for (int s = 0; s < 3; s++)
            {
                float y = height * .32f + s * height * .2f;
                Box(cabinet, null, new Vector3(0, y, depth * .5f + .02f), new Vector3(width - .12f, .015f, .04f), stainless);
                for (int i = 0; i < 4; i++)
                    if (dressing.NextDouble() < .7)
                        Box(cabinet, null, new Vector3(-width * .35f + i * width * .23f, y + .0825f, depth * .5f + .02f), new Vector3(.14f, .15f, .03f),
                            dressing.NextDouble() < .5 ? plasticGrey : enamel);
            }
            Box(cabinet, null, new Vector3(0, height * .58f, depth * .5f + .045f), new Vector3(width - .06f, height * .64f, .01f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(cabinet, null, new Vector3(0, height * .13f, depth * .5f + .005f), new Vector3(width - .04f, height * .22f, .012f), enamelTeal);
            Box(cabinet, null, new Vector3(width * .38f, height * .94f, depth * .5f + .004f), new Vector3(.04f, .02f, .004f), ledBlue);
        }

        static void Freezer(Transform parent, Vector3 at, float yaw, bool glazed)
        {
            var unit = Placed(parent, "Freezer", at, yaw);
            const float width = .86f, depth = .9f, height = 1.98f;
            Solid(unit, null, new Vector3(0, height * .5f + .06f, 0), new Vector3(width, height, depth), enamel);
            Box(unit, null, new Vector3(0, 1.0f, depth * .5f + .02f), new Vector3(width - .04f, 1.6f, .04f), glazed ? enamelTeal : enamel);
            if (glazed)
            {
                Box(unit, null, new Vector3(0, 1.08f, depth * .5f + .043f), new Vector3(width - .22f, 1.2f, .006f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Box(unit, null, new Vector3(0, 1.08f, depth * .5f + .036f), new Vector3(width - .26f, 1.16f, .004f), coldInterior);
                for (int r = 0; r < 4; r++) Box(unit, null, new Vector3(0, .62f + r * .3f, depth * .5f + .039f), new Vector3(width - .3f, .2f, .004f), steel);
            }
            Box(unit, null, new Vector3(width * .36f, 1.05f, depth * .5f + .06f), new Vector3(.04f, .48f, .05f), stainless);
            Box(unit, null, new Vector3(0, 1.92f, depth * .5f + .004f), new Vector3(.34f, .12f, .008f), darkMetal);
            Box(unit, null, new Vector3(-.05f, 1.92f, depth * .5f + .009f), new Vector3(.16f, .04f, .004f), ledBlue);
            Box(unit, null, new Vector3(.11f, 1.92f, depth * .5f + .009f), new Vector3(.025f, .025f, .004f), ledGreen);
            for (int i = 0; i < 5; i++) Box(unit, null, new Vector3(0, .1f + i * .035f, depth * .5f + .003f), new Vector3(width - .2f, .012f, .006f), darkMetal);
            Box(unit, null, new Vector3(0, 1.05f, -depth * .5f - .004f), new Vector3(width - .1f, 1.7f, .008f), plasticGrey);
            for (int i = 0; i < 14; i++) Box(unit, null, new Vector3(0, .35f + i * .05f, -depth * .5f - .01f), new Vector3(width - .22f, .012f, .006f), darkMetal);
            Box(unit, null, new Vector3(width * .3f, 1.55f, -depth * .5f - .012f), new Vector3(.1f, .07f, .01f), darkMetal);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Cylinder(unit, null, new Vector3(x * .34f, .035f, z * .36f), .035f, .05f, rubber, new Vector3(0, 0, 90));
        }

        static void Dewar(Transform parent, Vector3 at)
        {
            var tank = Placed(parent, "Tank", at, 0);
            Solid(tank, null, new Vector3(0, .62f, 0), new Vector3(.5f, 1.04f, .5f), stainless, false);
            Cylinder(tank, null, new Vector3(0, .62f, 0), .25f, 1.04f, stainless);
            Cylinder(tank, null, new Vector3(0, 1.17f, 0), .19f, .08f, stainless);
            Cylinder(tank, null, new Vector3(0, 1.26f, 0), .07f, .12f, steel);
            Cylinder(tank, null, new Vector3(.1f, 1.3f, .06f), .035f, .015f, paper, new Vector3(90, 0, 0));
            Cylinder(tank, null, new Vector3(0, .07f, 0), .29f, .05f, darkMetal);
            Cylinder(tank, null, new Vector3(0, .9f, 0), .252f, .05f, hazardStripe);
        }

        static void ServerRack(Transform parent, Vector3 at, float yaw, int seed)
        {
            var random = new System.Random(seed);
            var rack = Placed(parent, "Server", at, yaw);
            const float width = .62f, depth = 1.0f, height = 2.1f;
            Solid(rack, null, new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), darkMetal);
            Box(rack, null, new Vector3(0, height + .01f, 0), new Vector3(width + .01f, .02f, depth + .01f), steel);
            var leds = new List<Renderer>();
            for (int face = -1; face <= 1; face += 2)
            {
                float z = face * (depth * .5f + .005f);
                Box(rack, null, new Vector3(0, height * .5f, z), new Vector3(width - .02f, height - .04f, .01f), rubber);
                for (int u = 0; u < 14; u++)
                {
                    float y = .2f + u * .13f;
                    Box(rack, null, new Vector3(0, y, z + face * .004f), new Vector3(width - .1f, .1f, .004f), plasticGrey);
                    for (int l = 0; l < 3; l++)
                    {
                        double roll = random.NextDouble();
                        var led = Box(rack, null, new Vector3(width * .3f - l * .035f, y, z + face * .008f), new Vector3(.012f, .012f, .004f),
                            roll < .55 ? ledGreen : roll < .85 ? ledBlue : ledAmber);
                        if (random.NextDouble() < .5) leds.Add(led.GetComponent<Renderer>());
                    }
                }
            }
            var blinker = rack.gameObject.AddComponent<StatusBlinker>();
            blinker.lights = leds.ToArray(); blinker.seed = seed;
        }

        static void CableTray(Transform parent, Vector3 at, float length)
        {
            var tray = Placed(parent, "Cable Tray", at, 0);
            Box(tray, null, Vector3.zero, new Vector3(.4f, .012f, length), steel);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(tray, null, new Vector3(side * .2f, .03f, 0), new Vector3(.012f, .06f, length), steel);
                // Threaded rods up to the ceiling slab.
                for (int end = -1; end <= 1; end += 2)
                    Cylinder(tray, null, new Vector3(side * .17f, (Ceiling - at.y) * .5f, end * length * .38f), .008f, Ceiling - at.y, steel);
            }
            Box(tray, null, new Vector3(0, .03f, 0), new Vector3(.3f, .04f, length - .1f), rubber);
        }

        /// <summary>A pipe run hung from the ceiling on rods and clamps.</summary>
        static void Pipe(Transform parent, Vector3 from, Vector3 to, float radius, Material material)
        {
            var run = Child(parent, "Pipe");
            Vector3 mid = (from + to) * .5f, delta = to - from;
            var pipe = Primitive(run, null, PrimitiveType.Cylinder, mid, new Vector3(radius * 2, delta.magnitude * .5f, radius * 2), material, false);
            pipe.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            pipe.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            int hangers = Mathf.Max(2, Mathf.RoundToInt(delta.magnitude / 2.2f));
            for (int i = 0; i < hangers; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (i + .5f) / hangers);
                float top = p.y + radius;
                var rod = Cylinder(run, null, new Vector3(p.x, (top + Ceiling) * .5f, p.z), .006f, Ceiling - top, steel);
                var clamp = Primitive(run, null, PrimitiveType.Cylinder, p, new Vector3(radius * 2 + .012f, .012f, radius * 2 + .012f), steel, false);
                clamp.transform.localRotation = pipe.transform.localRotation;
                rod.GetComponent<Renderer>().shadowCastingMode = clamp.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void WallSign(Transform parent, Vector3 at, float yaw, Material face, float size)
        {
            var sign = Placed(parent, "Sign", at, yaw);
            Box(sign, null, new Vector3(0, 0, .0075f), new Vector3(size + .03f, size + .03f, .015f), darkMetal);
            Box(sign, null, new Vector3(0, 0, .0165f), new Vector3(size, size, .003f), face);
        }

        static void Extinguisher(Transform parent, Vector3 at, float yaw)
        {
            var unit = Placed(parent, "Fire Extinguisher", at, yaw);
            Box(unit, null, new Vector3(0, .1f, .01f), new Vector3(.1f, .3f, .02f), darkMetal);
            Cylinder(unit, null, new Vector3(0, 0, .095f), .075f, .5f, extinguisherRed);
            Cylinder(unit, null, new Vector3(0, .29f, .095f), .025f, .08f, darkMetal);
            Box(unit, null, new Vector3(0, .34f, .125f), new Vector3(.03f, .02f, .12f), darkMetal);
            Cylinder(unit, null, new Vector3(.07f, .1f, .125f), .012f, .34f, rubber);
        }

        static void WallClock(Transform parent, Vector3 at, float yaw)
        {
            var clock = Placed(parent, "Clock", at, yaw);
            Cylinder(clock, null, new Vector3(0, 0, .025f), .16f, .05f, darkMetal, AlongZ);
            Cylinder(clock, null, new Vector3(0, 0, .051f), .145f, .004f, paper, AlongZ);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30 * Mathf.Deg2Rad;
                Box(clock, null, new Vector3(Mathf.Sin(a) * .12f, Mathf.Cos(a) * .12f, .054f), new Vector3(.008f, i % 3 == 0 ? .03f : .015f, .003f), darkMetal)
                    .transform.localRotation = Quaternion.Euler(0, 0, -i * 30);
            }
            Box(clock, null, new Vector3(.03f, .02f, .056f), new Vector3(.012f, .08f, .003f), darkMetal).transform.localRotation = Quaternion.Euler(0, 0, -62);
            Box(clock, null, new Vector3(-.04f, .03f, .058f), new Vector3(.008f, .12f, .003f), darkMetal).transform.localRotation = Quaternion.Euler(0, 0, 52);
        }

        static void Dispenser(Transform parent, Vector3 at, float yaw)
        {
            var unit = Placed(parent, "Dispenser", at, yaw);
            Box(unit, null, new Vector3(0, 0, .045f), new Vector3(.12f, .22f, .09f), enamel);
            Box(unit, null, new Vector3(0, .02f, .0915f), new Vector3(.06f, .08f, .003f), accentTeal);
            Box(unit, null, new Vector3(0, -.13f, .065f), new Vector3(.1f, .02f, .07f), plasticGrey);
        }

        static void CardReader(Transform parent, Vector3 at, float yaw, Material state)
        {
            var reader = Placed(parent, "Card Reader", at, yaw);
            Box(reader, null, new Vector3(0, 0, .015f), new Vector3(.09f, .14f, .03f), darkMetal);
            Box(reader, null, new Vector3(0, -.02f, .031f), new Vector3(.06f, .06f, .003f), plasticGrey);
            Box(reader, null, new Vector3(0, .05f, .032f), new Vector3(.04f, .012f, .003f), state);
        }

        // ------------------------------------------------------------ CCTV
        /// <summary>
        /// Bullet camera on a swivel. <paramref name="mount"/> points from the camera to the wall it is bolted to,
        /// or up for a ceiling drop rod; <paramref name="reach"/> is the distance to that surface.
        /// </summary>
        static HazardDetector Scanner(Transform root, StealthGame game, DetectionShape shape, Vector3 at, Vector3 target, float angle, float range, float sweep, float period, float phase, Vector3 mount, float reach)
        {
            var rig = Child(root, "CCTV"); rig.position = at; rig.gameObject.layer = ScannerLayer;
            var detector = rig.gameObject.AddComponent<HazardDetector>(); detector.shape = shape; detector.home = at; detector.lateralAmplitude = 0;
            detector.player = game.body.transform; detector.playerMask = 1 << PlayerLayer; detector.obstructionMask = Obstructions;
            detector.useViewCone = true; detector.viewAngle = angle; detector.viewRange = range;
            var pivot = Child(rig, "Head");
            detector.panPivot = pivot; detector.panBaseEuler = Quaternion.LookRotation(target - at).eulerAngles;
            detector.panAmplitude = sweep; detector.panPeriod = period; detector.panPhase = phase;
            var origin = Child(pivot, "Lens"); detector.rayOrigin = origin;
            float r = shape == DetectionShape.Box ? .07f : .062f;
            float length = shape == DetectionShape.Box ? .42f : shape == DetectionShape.Capsule ? .36f : .32f;

            // Fixed bracket: the swivel sits on the pan axis just above the housing, so the head turns under it.
            var bracket = Child(rig, "Mount");
            Cylinder(bracket, null, new Vector3(0, r + .03f, 0), .028f, .025f, cameraDark);
            if (mount == Vector3.up)
            {
                Cylinder(bracket, null, new Vector3(0, (reach + r + .04f) * .5f, 0), .016f, reach - r - .04f, cameraShell);
                Cylinder(bracket, null, new Vector3(0, reach - .008f, 0), .075f, .016f, cameraDark);
            }
            else
            {
                float armY = r + .07f;
                Cylinder(bracket, null, new Vector3(0, (r + .04f + armY) * .5f, 0), .018f, armY - r - .02f, cameraShell);
                var arm = Box(bracket, null, new Vector3(0, armY, 0) + mount * reach * .5f, new Vector3(.04f, .04f, reach), cameraShell);
                arm.transform.localRotation = Quaternion.LookRotation(mount);
                var plate = Box(bracket, null, new Vector3(0, armY, 0) + mount * (reach - .008f), new Vector3(.12f, .17f, .016f), cameraDark);
                plate.transform.localRotation = Quaternion.LookRotation(mount);
            }

            // Moving head: housing, sun shield and lens share the optical pivot, so the camera follows the light.
            var housing = Child(pivot, "Camera");
            float back = .07f - length;
            Cylinder(housing, null, new Vector3(0, 0, .07f - length * .5f), r, length, cameraShell, AlongZ);
            Cylinder(housing, null, new Vector3(0, 0, .065f), r + .006f, .03f, cameraDark, AlongZ);
            Cylinder(housing, null, new Vector3(0, 0, back - .008f), r + .003f, .02f, cameraDark, AlongZ);
            var optic = Cylinder(housing, null, new Vector3(0, 0, .0805f), r * .82f, .003f, lensGlass, AlongZ);
            Cylinder(housing, null, new Vector3(0, 0, .0812f), r * .36f, .003f, cameraDark, AlongZ);
            float shield = length + .07f, shieldZ = .12f - shield * .5f;
            Box(housing, null, new Vector3(0, r + .012f, shieldZ), new Vector3(r * 2 + .03f, .008f, shield), cameraShell);
            for (int side = -1; side <= 1; side += 2)
                Box(housing, null, new Vector3(side * (r + .012f), r * .5f, shieldZ), new Vector3(.006f, r * 1.05f, shield), cameraShell);
            Cylinder(housing, null, new Vector3(0, -r * .45f, back - .035f), .009f, .06f, rubber, AlongZ);
            origin.localPosition = new Vector3(0, 0, .082f);
            var status = Primitive(housing, null, PrimitiveType.Sphere, new Vector3(r * .55f, r * .5f, .081f), Vector3.one * .014f, recording, false).GetComponent<Renderer>();

            var visual = rig.gameObject.AddComponent<ScannerVisual>(); visual.detector = detector; visual.game = game; visual.lineMaterial = line; visual.shaftMaterial = shaft;
            visual.lens = optic.GetComponent<Renderer>(); visual.statusLights = new[] { status };
            var search = Child(origin, "Spot Light");
            var lamp = search.gameObject.AddComponent<Light>(); lamp.type = LightType.Spot; lamp.range = range; lamp.spotAngle = angle; lamp.innerSpotAngle = angle * ScannerVisual.InnerAngle;
            lamp.color = new Color(.86f, .93f, 1f); lamp.intensity = Mathf.Lerp(150, 95, Mathf.InverseLerp(34, 68, angle)) * range * range / 100f;
            lamp.shadows = LightShadows.Soft; lamp.shadowBias = .015f; lamp.shadowNormalBias = .04f; lamp.shadowNearPlane = .05f;
            lamp.cullingMask = ~(1 << ScannerLayer); visual.lamp = lamp;
            var lampData = new SerializedObject(search.gameObject.AddComponent<UniversalAdditionalLightData>());
            lampData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            lampData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            SetLayer(rig.gameObject, ScannerLayer); detector.SetPatrolTime(0); return detector;
        }

        // ------------------------------------------------------------ held flashlight
        static Transform HeldFlashlight(Transform camera)
        {
            var hand = Child(camera, "Flashlight"); hand.localPosition = new Vector3(.16f, -.17f, .32f);
            hand.localRotation = Quaternion.Euler(-2, -3, 0);
            var torch = Child(hand, "Torch");
            Cylinder(torch, null, new Vector3(0, 0, -.02f), .0165f, .13f, darkMetal, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, -.03f), .0176f, .056f, rubber, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, .055f), .019f, .022f, darkMetal, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, .085f), .024f, .04f, darkMetal, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, .108f), .0255f, .008f, steel, AlongZ);
            var lens = Cylinder(torch, null, new Vector3(0, 0, .1125f), .0205f, .002f, torchLens, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, -.093f), .018f, .018f, darkMetal, AlongZ);
            Cylinder(torch, null, new Vector3(0, 0, -.104f), .009f, .006f, rubber, AlongZ);
            GlovedHand(hand);
            var muzzle = Child(hand, "Muzzle"); muzzle.localPosition = new Vector3(0, 0, .12f);
            muzzle.rotation = camera.rotation;
            var visual = hand.gameObject.AddComponent<HeldFlashlightVisual>(); visual.lampGlass = lens.GetComponent<Renderer>(); visual.poseRoot = hand;
            foreach (var r in hand.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            SetLayer(hand.gameObject, PlayerLayer); return muzzle;
        }

        /// <summary>
        /// Right hand closed round the grip, seen from the barrel's front: the back of the hand lies along the right,
        /// four three-joint fingers curl from the knuckles over the top and down the left, and the thumb runs forward
        /// along the side. Points are (angle round the barrel, distance from its axis, position along it).
        /// </summary>
        static void GlovedHand(Transform parent)
        {
            var hand = Child(parent, "Hand");
            Vector3 At(float degrees, float radius, float z) =>
                new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad) * radius, Mathf.Sin(degrees * Mathf.Deg2Rad) * radius, z);
            for (int finger = 0; finger < 4; finger++)
            {
                float z = -.006f - finger * .0182f, scale = finger == 3 ? .86f : finger == 0 ? .96f : 1f;
                Vector3 knuckle = At(32, .035f, z), middle = At(102, .0285f, z - .001f);
                Vector3 joint = At(168, .0275f, z - .002f), tip = At(finger == 3 ? 212 : 226, .027f, z - .003f);
                Primitive(hand, null, PrimitiveType.Sphere, knuckle, Vector3.one * .023f * scale, glove, false);
                Limb(hand, knuckle, middle, .0098f * scale, glove);
                Limb(hand, middle, joint, .0093f * scale, glove);
                Limb(hand, joint, tip, .0086f * scale, glove);
            }
            // Back of the hand: a rounded slab from the knuckle line down to the wrist.
            Vector3 knuckles = At(32, .036f, -.033f), wrist = At(-58, .068f, -.04f);
            var back = Primitive(hand, null, PrimitiveType.Capsule, (knuckles + wrist) * .5f + new Vector3(.006f, 0, 0),
                new Vector3(.03f, (wrist - knuckles).magnitude * .62f, .082f), glove, false);
            back.transform.localRotation = Quaternion.LookRotation(Vector3.forward, knuckles - wrist);
            Limb(hand, At(-22, .034f, -.004f), At(8, .03f, .03f), .0105f, glove);
            Primitive(hand, null, PrimitiveType.Sphere, At(-30, .04f, -.012f), new Vector3(.03f, .032f, .036f), glove, false);
            // Cuff and sleeve run back and down out of view.
            Vector3 forearm = new Vector3(.14f, -.23f, -.34f), cuff = Vector3.Lerp(wrist, forearm, .12f);
            Limb(hand, wrist, cuff, .029f, glove);
            Limb(hand, cuff, Vector3.Lerp(wrist, forearm, .2f), .032f, rubber);
            Limb(hand, Vector3.Lerp(wrist, forearm, .19f), forearm, .04f, sleeve);
        }

        // ------------------------------------------------------------ clue surfaces
        /// <summary>
        /// Registers a prop as a clue surface. The face sits on the writable plane with +Z pointing into the surface,
        /// so lettering written in its XY plane reads from the front.
        /// </summary>
        static ClueSpot Spot(Transform prop, Vector3 surface, Quaternion faceRotation, ClueMedium medium, int zone, bool allowOrder, Vector2 area, Material ink)
        {
            var face = Child(prop, "Clue"); face.localPosition = surface; face.localRotation = faceRotation;
            face.gameObject.layer = ClueLayer;
            var spot = prop.gameObject.AddComponent<ClueSpot>();
            spot.medium = medium; spot.zone = zone; spot.allowOrder = allowOrder; spot.face = face; spot.area = area; spot.ink = ink;
            var clue = prop.gameObject.AddComponent<HiddenClue>();
            clue.anchor = face; clue.obstructionMask = Obstructions; clue.hiddenRenderers = new Renderer[0];
            spot.clue = clue;
            spots.Add(spot);
            return spot;
        }

        static readonly Quaternion FacingFront = Quaternion.Euler(0, 180, 0);

        static void Plaque(Transform parent, int zone, Vector3 at, float yaw)
        {
            var sign = Placed(parent, "Sign", at, yaw);
            Box(sign, null, new Vector3(0, 0, .006f), new Vector3(.3f, .4f, .012f), darkMetal);
            Box(sign, null, new Vector3(0, 0, .0105f), new Vector3(.31f, .41f, .004f), stainless);
            Box(sign, null, new Vector3(0, 0, .0124f), new Vector3(.29f, .39f, .002f), darkMetal);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    Cylinder(sign, null, new Vector3(x * .125f, y * .17f, .014f), .006f, .003f, stainless, AlongZ);
            Spot(sign, new Vector3(0, 0, .0142f), FacingFront, ClueMedium.Plaque, zone, true, new Vector2(.25f, .34f), inkEngrave);
        }

        static void StickyNote(Transform parent, int zone, Vector3 at, float yaw, int seed)
        {
            var random = new System.Random(seed);
            var note = Placed(parent, "Sticky Note", at, yaw);
            note.localRotation *= Quaternion.Euler(0, 0, (float)(random.NextDouble() - .5) * 12);
            Box(note, null, new Vector3(0, 0, .0008f), new Vector3(.1f, .1f, .0016f), stickyYellow);
            Box(note, null, new Vector3(0, .042f, .0019f), new Vector3(.1f, .016f, .0002f), stickyGlue);
            Spot(note, new Vector3(0, -.004f, .0024f), FacingFront, ClueMedium.StickyNote, zone, false, new Vector2(.09f, .08f), inkMarker);
        }

        static void Clipboard(Transform parent, int zone, Vector3 at, float yaw, int seed)
        {
            var random = new System.Random(seed);
            var board = Placed(parent, "Clipboard", at, yaw);
            board.localRotation *= Quaternion.Euler(0, 0, (float)(random.NextDouble() - .5) * 4);
            Box(board, null, new Vector3(0, 0, .003f), new Vector3(.25f, .35f, .006f), clipboardBoard);
            Box(board, null, new Vector3(0, -.02f, .0068f), new Vector3(.215f, .29f, .0015f), paper);
            Box(board, null, new Vector3(0, .156f, .012f), new Vector3(.1f, .032f, .012f), stainless);
            Cylinder(board, null, new Vector3(0, .176f, .009f), .014f, .006f, darkMetal, AlongZ);
            // Printed form: a header bar and ruled lines along the bottom.
            Box(board, null, new Vector3(0, .108f, .0081f), new Vector3(.185f, .012f, .0003f), plasticGrey);
            for (int i = 0; i < 3; i++) Box(board, null, new Vector3(0, -.118f - i * .015f, .0081f), new Vector3(.185f, .0015f, .0003f), plasticGrey);
            Spot(board, new Vector3(0, -.005f, .0088f), FacingFront, ClueMedium.Clipboard, zone, true, new Vector2(.19f, .19f), inkBallpoint);
        }

        /// <summary>A loose sheet on a worktop; <paramref name="readerYaw"/> is the direction someone reading it looks.</summary>
        static void DeskPaper(Transform parent, int zone, Vector3 at, float readerYaw)
        {
            var sheet = Placed(parent, "Paper", at, readerYaw);
            Box(sheet, null, new Vector3(0, .0005f, 0), new Vector3(.21f, .001f, .297f), paper);
            for (int i = 0; i < 6; i++)
                Box(sheet, null, new Vector3(i == 0 ? -.03f : 0, .0014f, .12f - i * .013f), new Vector3(i == 0 ? .1f : .16f, .0002f, i == 0 ? .006f : .0025f), plasticGrey);
            Spot(sheet, new Vector3(0, .0019f, -.045f), Quaternion.LookRotation(Vector3.down, Vector3.forward), ClueMedium.Paper, zone, true, new Vector2(.18f, .17f), inkPencil);
        }

        /// <summary>The reserved screen on a lab island's -z face.</summary>
        static void BenchScreen(Transform bench, int zone, float along)
        {
            var monitor = Monitor(bench, new Vector3(along, .94f, -.4f), 180, screenOff);
            Spot(monitor, new Vector3(0, .36f, .0028f), FacingFront, ClueMedium.Monitor, zone, true, new Vector2(.46f, .26f), inkScreen);
        }

        static void WallMonitorSpot(int zone, Transform parent, Vector3 at, float yaw)
        {
            var monitor = WallMonitor(parent, at, yaw, screenOff);
            Spot(monitor, new Vector3(0, 0, .0528f), FacingFront, ClueMedium.Monitor, zone, true, new Vector2(.46f, .26f), inkScreen);
        }

        static void Whiteboard(Transform parent, int zone, Vector3 at, float yaw)
        {
            var board = Placed(parent, "Whiteboard", at, yaw);
            Box(board, null, new Vector3(0, 0, .01f), new Vector3(1.54f, .99f, .02f), doorFrame);
            Box(board, null, new Vector3(0, 0, .0205f), new Vector3(1.5f, .95f, .002f), whiteboard);
            Box(board, null, new Vector3(0, -.49f, .035f), new Vector3(1.0f, .015f, .05f), doorFrame);
            Box(board, null, new Vector3(0, -.475f, .058f), new Vector3(1.0f, .02f, .006f), doorFrame);
            for (int i = 0; i < 2; i++)
                Cylinder(board, null, new Vector3(-.3f + i * .12f, -.474f, .035f), .009f, .12f, i == 0 ? binderRed : binderBlue, new Vector3(0, 0, 90));
            Box(board, null, new Vector3(.3f, -.466f, .035f), new Vector3(.12f, .03f, .045f), rubber);
            // Old notes on the left half stay visible; the right half is left for a clue.
            var doodle = Child(board, "Notes"); doodle.localPosition = new Vector3(.38f, .03f, .0222f); doodle.localRotation = FacingFront;
            var mesh = new LetteringMesh();
            var random = new System.Random(5);
            Vector2[] boxes = { new Vector2(-.24f, .24f), new Vector2(.02f, .24f), new Vector2(.26f, .24f) };
            string[] steps = { "PREP", "SPIN", "QC" };
            for (int i = 0; i < boxes.Length; i++)
            {
                Vector2 c = boxes[i], h = new Vector2(.09f, .05f);
                mesh.Stroke(new[] { c + new Vector2(-h.x, -h.y), c + new Vector2(-h.x, h.y), c + new Vector2(h.x, h.y), c + new Vector2(h.x, -h.y), c + new Vector2(-h.x, -h.y) }, .006f, true);
                Lettering.StrokeText(mesh, steps[i], c + new Vector2(-Lettering.StrokeWidth(steps[i], .035f) * .5f, -.0175f), .035f, LetteringStyle.Marker, random);
                if (i < boxes.Length - 1)
                {
                    mesh.Stroke(new[] { c + new Vector2(h.x + .01f, 0), c + new Vector2(.26f - h.x - .015f, 0) }, .005f, true);
                    mesh.Stroke(new[] { c + new Vector2(.26f - h.x - .035f, .015f), c + new Vector2(.26f - h.x - .015f, 0), c + new Vector2(.26f - h.x - .035f, -.015f) }, .005f, true);
                }
            }
            var curve = new Vector2[24];
            for (int i = 0; i < curve.Length; i++) { float t = i / 23f; curve[i] = new Vector2(-.3f + t * .56f, -.2f + Mathf.Sin(t * 5.5f) * .06f + t * .08f); }
            mesh.Stroke(curve, .005f, true);
            mesh.Stroke(new[] { new Vector2(-.32f, -.05f), new Vector2(-.32f, -.31f), new Vector2(.3f, -.31f) }, .005f, true);
            Lettering.StrokeText(mesh, "TEMP -80", new Vector2(-.3f, .06f), .03f, LetteringStyle.Marker, random);
            var notes = doodle.gameObject.AddComponent<MeshFilter>(); notes.sharedMesh = mesh.Build("Whiteboard notes");
            var notesRenderer = doodle.gameObject.AddComponent<MeshRenderer>(); notesRenderer.sharedMaterial = inkWhiteboard;
            notesRenderer.shadowCastingMode = ShadowCastingMode.Off;
            Spot(board, new Vector3(-.36f, .02f, .0222f), FacingFront, ClueMedium.Whiteboard, zone, true, new Vector2(.64f, .7f), inkMarkerRed);
        }

        static void ControlPanel(Transform parent, int zone, Vector3 at, float yaw)
        {
            var panel = Placed(parent, "Control Panel", at, yaw);
            Box(panel, null, new Vector3(0, 0, .03f), new Vector3(.42f, .32f, .06f), enamel);
            Box(panel, null, new Vector3(0, .05f, .061f), new Vector3(.34f, .16f, .004f), darkMetal);
            Box(panel, null, new Vector3(0, .05f, .0635f), new Vector3(.3f, .12f, .002f), lcdBacklight);
            for (int i = 0; i < 3; i++) Box(panel, null, new Vector3(-.1f + i * .1f, -.09f, .065f), new Vector3(.06f, .035f, .01f), plasticGrey);
            Box(panel, null, new Vector3(.17f, -.13f, .061f), new Vector3(.015f, .015f, .004f), ledBlue);
            // Conduit from the top of the box up to the ceiling.
            Cylinder(panel, null, new Vector3(-.14f, (Ceiling - at.y + .16f) * .5f, .02f), .012f, Ceiling - at.y - .16f, steel);
            Spot(panel, new Vector3(0, .05f, .0652f), FacingFront, ClueMedium.Lcd, zone, true, new Vector2(.29f, .11f), inkLcd);
        }

        static void FreezerLabel(Transform parent, int zone, Vector3 at, float yaw)
        {
            var label = Placed(parent, "Label", at, yaw);
            Box(label, null, new Vector3(0, 0, .0004f), new Vector3(.26f, .055f, .0008f), labelTape);
            Spot(label, new Vector3(0, 0, .0013f), FacingFront, ClueMedium.Label, zone, false, new Vector2(.25f, .05f), inkLabel);
        }

        static void Stencil(Transform parent, int zone, Vector3 at, float yaw)
        {
            var stencil = Placed(parent, "Stencil", at, yaw);
            Spot(stencil, new Vector3(0, 0, .0015f), FacingFront, ClueMedium.Stencil, zone, true, new Vector2(1.0f, .7f), inkPaint);
        }

        static void Memo(Transform parent, int zone, Vector3 at, float yaw)
        {
            var memo = Placed(parent, "Memo", at, yaw);
            Box(memo, null, new Vector3(0, 0, .0004f), new Vector3(.21f, .297f, .0008f), paper);
            for (int side = -1; side <= 1; side += 2)
                Box(memo, null, new Vector3(side * .09f, .14f, .0009f), new Vector3(.065f, .022f, .0004f), tape).transform.localRotation = Quaternion.Euler(0, 0, side * -32);
            Box(memo, null, new Vector3(0, .115f, .0013f), new Vector3(.17f, .012f, .0002f), plasticGrey);
            Spot(memo, new Vector3(0, -.02f, .0018f), FacingFront, ClueMedium.Memo, zone, true, new Vector2(.18f, .22f), inkPrint);
        }
    }
}
