using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning.Editor
{
    /// <summary>Locally authored laboratory props. Each prop faces local +z with its footprint centred on the origin.</summary>
    public static partial class ZoningBuilder
    {
        static Material extinguisherRed;
        static System.Random dressing;

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

        static GameObject GlassPane(Transform parent, string name, Vector3 at, Vector3 size)
        {
            var go = Primitive(parent, name, PrimitiveType.Cube, at, size, glass, true);
            go.layer = GlassLayer;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void LockerBank(Transform parent, Vector3 at, float yaw, int count)
        {
            var bank = Placed(parent, "Staff locker bank", at, yaw);
            float width = count * .42f;
            Solid(bank, "Locker bank body", new Vector3(0, 1.05f, 0), new Vector3(width, 1.9f, .5f), lockerPaint);
            Box(bank, "Locker plinth", new Vector3(0, .05f, -.02f), new Vector3(width, .1f, .46f), skirting);
            Box(bank, "Locker crown", new Vector3(0, 2.02f, 0), new Vector3(width + .02f, .04f, .52f), lockerPaint);
            for (int i = 0; i < count; i++)
            {
                float x = -width * .5f + .21f + i * .42f;
                Box(bank, "Locker door", new Vector3(x, 1.06f, .255f), new Vector3(.40f, 1.82f, .018f), i % 3 == 1 ? enamelTeal : lockerPaint);
                for (int k = 0; k < 4; k++) Box(bank, "Door vent", new Vector3(x, 1.68f + k * .045f, .266f), new Vector3(.22f, .016f, .006f), darkMetal);
                Box(bank, "Locker handle", new Vector3(x + .13f, 1.02f, .272f), new Vector3(.024f, .14f, .03f), stainless);
                Box(bank, "Name card", new Vector3(x, 1.5f, .266f), new Vector3(.11f, .045f, .005f), paper);
            }
        }

        static void ChangingBench(Transform parent, Vector3 at, float yaw, float length)
        {
            var bench = Placed(parent, "Changing bench", at, yaw);
            Solid(bench, "Bench seat", new Vector3(0, .43f, 0), new Vector3(length, .05f, .36f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(bench, "Bench frame", new Vector3(side * (length * .5f - .12f), .21f, 0), new Vector3(.05f, .42f, .32f), stainless);
                Box(bench, "Bench foot", new Vector3(side * (length * .5f - .12f), .015f, 0), new Vector3(.08f, .03f, .36f), darkMetal);
            }
        }

        static void SecurityDesk(Transform parent, Vector3 at, float yaw, float width)
        {
            var desk = Placed(parent, "Night security desk", at, yaw);
            Solid(desk, "Reception counter", new Vector3(0, .53f, .1f), new Vector3(width, 1.06f, .5f), enamel);
            Box(desk, "Counter accent", new Vector3(0, .62f, .355f), new Vector3(width, .08f, .012f), accentTeal);
            Box(desk, "Counter cap", new Vector3(0, 1.08f, .14f), new Vector3(width + .04f, .04f, .6f), epoxy);
            Solid(desk, "Operator desk", new Vector3(0, .37f, -.35f), new Vector3(width - .2f, .74f, .62f), plasticGrey);
            Box(desk, "Operator worktop", new Vector3(0, .755f, -.35f), new Vector3(width - .16f, .03f, .66f), wood);
            Material[] feeds = { screenFeed, screenGraph, screenFeed };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * .62f;
                var monitor = Placed(desk, "Surveillance monitor", new Vector3(x, .78f, -.22f), 180 + (i - 1) * 14);
                Box(monitor, "Monitor stand", new Vector3(0, .1f, .06f), new Vector3(.05f, .2f, .05f), darkMetal);
                Box(monitor, "Monitor foot", new Vector3(0, .01f, .06f), new Vector3(.22f, .02f, .16f), darkMetal);
                Box(monitor, "Monitor bezel", new Vector3(0, .38f, .02f), new Vector3(.56f, .34f, .035f), darkMetal);
                Box(monitor, "Monitor display", new Vector3(0, .38f, .039f), new Vector3(.52f, .30f, .004f), feeds[i]);
            }
            Box(desk, "Keyboard", new Vector3(0, .78f, -.52f), new Vector3(.42f, .02f, .14f), darkMetal);
            Box(desk, "Desk lamp base", new Vector3(width * .5f - .25f, .78f, -.5f), new Vector3(.12f, .02f, .12f), darkMetal);
            var chair = Placed(desk, "Empty operator chair", new Vector3(.15f, 0, -1.05f), 160);
            Cylinder(chair, "Chair column", new Vector3(0, .24f, 0), .03f, .4f, stainless);
            Box(chair, "Chair seat", new Vector3(0, .48f, 0), new Vector3(.48f, .07f, .46f), rubber);
            Box(chair, "Chair back", new Vector3(0, .82f, -.22f), new Vector3(.44f, .52f, .06f), rubber);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72;
                Box(chair, "Chair leg", Quaternion.Euler(0, a, 0) * new Vector3(0, .05f, .18f), new Vector3(.04f, .03f, .34f), darkMetal).transform.localRotation = Quaternion.Euler(0, a, 0);
            }
        }

        static void Shelving(Transform parent, Vector3 at, float yaw, float width, float height)
        {
            var shelf = Placed(parent, "Supply shelving divider", at, yaw);
            Solid(shelf, "Shelving back panel", new Vector3(0, height * .5f, 0), new Vector3(width, height, .05f), enamel);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box(shelf, "Shelf upright", new Vector3(x * (width * .5f - .02f), height * .5f, z * .23f), new Vector3(.04f, height, .04f), steel);
            Material[] binders = { binderBlue, binderRed, binderYellow, paper, plasticGrey };
            for (int level = 0; level < 5; level++)
            {
                float y = .12f + level * (height - .3f) / 4;
                Box(shelf, "Steel shelf", new Vector3(0, y, 0), new Vector3(width, .025f, .5f), steel);
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
                        Box(shelf, box ? "Sample box" : "Binder", new Vector3(x + w * .5f, y + .0125f + h * .5f, side * .13f), new Vector3(w, h, box ? .3f : .22f),
                            box ? (dressing.NextDouble() < .5 ? paper : plasticGrey) : binders[dressing.Next(binders.Length)]);
                        x += w + .015f;
                    }
                }
            }
        }

        static void LabBench(Transform parent, Vector3 at, float yaw, float length, int seed)
        {
            var random = new System.Random(seed);
            var bench = Placed(parent, "Analytical bench island", at, yaw);
            const float depth = 1.5f;
            Solid(bench, "Bench cabinets", new Vector3(0, .45f, 0), new Vector3(length, .9f, depth), enamel);
            Box(bench, "Bench plinth", new Vector3(0, .05f, 0), new Vector3(length - .04f, .1f, depth - .08f), skirting);
            Box(bench, "Epoxy worktop", new Vector3(0, .92f, 0), new Vector3(length + .04f, .04f, depth + .04f), epoxy);
            Solid(bench, "Service spine", new Vector3(0, 1.24f, 0), new Vector3(length, .64f, .22f), enamelTeal);
            Box(bench, "Spine cap", new Vector3(0, 1.57f, 0), new Vector3(length + .02f, .02f, .24f), stainless);
            int doors = Mathf.Max(2, Mathf.RoundToInt(length / .6f));
            for (int side = -1; side <= 1; side += 2)
            {
                float face = side * (depth * .5f + .004f);
                for (int i = 0; i <= doors; i++)
                {
                    float x = -length * .5f + i * length / doors;
                    if (i > 0 && i < doors) Box(bench, "Door seam", new Vector3(x, .5f, face), new Vector3(.008f, .76f, .006f), darkMetal);
                    if (i < doors) Box(bench, "Door pull", new Vector3(x + length / doors * .5f, .78f, face + side * .012f), new Vector3(.16f, .014f, .022f), stainless);
                }
                for (int i = 0; i < Mathf.Max(1, (int)(length / 1.1f)); i++)
                    Box(bench, "Spine socket", new Vector3(-length * .5f + .5f + i * 1.1f, 1.2f, side * .112f), new Vector3(.14f, .08f, .006f), paper);
            }
            Box(bench, "Reagent shelf", new Vector3(0, 1.82f, 0), new Vector3(length - .1f, .02f, .32f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 3; i++)
                Box(bench, "Shelf bracket", new Vector3(-length * .45f + i * length * .45f, 1.7f, 0), new Vector3(.03f, .24f, .3f), stainless);
            for (int i = 0; i < (int)(length / .22f); i++)
            {
                if (random.NextDouble() < .35) continue;
                float h = .1f + (float)random.NextDouble() * .12f;
                float x = -length * .5f + .15f + i * .22f;
                Cylinder(bench, "Reagent bottle", new Vector3(x, 1.83f + h * .5f, (float)random.NextDouble() * .16f - .08f), .026f + (float)random.NextDouble() * .012f, h * .8f, random.NextDouble() < .55 ? amberGlass : glass);
            }
            // Instruments on both work faces.
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * .45f;
                float x = -length * .5f + .45f;
                while (x < length * .5f - .4f)
                {
                    int kind = random.Next(5);
                    var item = Placed(bench, "Bench instrument", new Vector3(x, .94f, z), side > 0 ? 0 : 180);
                    if (kind == 0) { Microscope(item); x += .7f; }
                    else if (kind == 1) { BenchMonitor(item, random.NextDouble() < .5 ? screenGraph : screenMolecule); x += .8f; }
                    else if (kind == 2) { Centrifuge(item); x += .65f; }
                    else if (kind == 3) { Glassware(item, random); x += .55f; }
                    else { x += .45f; }
                }
            }
        }

        static void Microscope(Transform item)
        {
            Box(item, "Microscope base", new Vector3(0, .02f, 0), new Vector3(.2f, .04f, .26f), enamel);
            Box(item, "Microscope arm", new Vector3(0, .17f, -.08f), new Vector3(.06f, .3f, .07f), enamel);
            Box(item, "Microscope stage", new Vector3(0, .14f, .03f), new Vector3(.16f, .015f, .14f), darkMetal);
            Cylinder(item, "Objective turret", new Vector3(0, .25f, .02f), .045f, .05f, darkMetal);
            Cylinder(item, "Eyepiece tube", new Vector3(0, .34f, -.03f), .02f, .16f, darkMetal, new Vector3(-35, 0, 0));
        }

        static void BenchMonitor(Transform item, Material screen)
        {
            Box(item, "Monitor foot", new Vector3(0, .01f, -.1f), new Vector3(.2f, .02f, .14f), darkMetal);
            Box(item, "Monitor neck", new Vector3(0, .12f, -.12f), new Vector3(.04f, .22f, .03f), darkMetal);
            Box(item, "Monitor bezel", new Vector3(0, .33f, -.1f), new Vector3(.52f, .32f, .03f), darkMetal);
            Box(item, "Monitor display", new Vector3(0, .33f, -.084f), new Vector3(.48f, .28f, .004f), screen);
        }

        static void Centrifuge(Transform item)
        {
            Box(item, "Centrifuge body", new Vector3(0, .12f, 0), new Vector3(.42f, .24f, .42f), enamel);
            Cylinder(item, "Centrifuge lid", new Vector3(0, .245f, -.02f), .17f, .02f, plasticGrey);
            Box(item, "Centrifuge panel", new Vector3(0, .12f, .212f), new Vector3(.24f, .08f, .004f), darkMetal);
            Box(item, "Centrifuge status", new Vector3(.08f, .12f, .215f), new Vector3(.03f, .02f, .004f), ledGreen);
        }

        static void Glassware(Transform item, System.Random random)
        {
            Box(item, "Tube rack", new Vector3(0, .04f, 0), new Vector3(.3f, .08f, .1f), plasticGrey);
            for (int i = 0; i < 6; i++) Cylinder(item, "Sample tube", new Vector3(-.12f + i * .048f, .1f, 0), .012f, .12f, i % 2 == 0 ? glass : binderBlue);
            Cylinder(item, "Beaker", new Vector3(.24f, .07f, .05f), .05f, .14f, glass);
            Cylinder(item, "Flask base", new Vector3(-.26f, .05f, .06f), .06f, .1f, glass);
        }

        static void FumeHood(Transform parent, Vector3 at, float yaw, float width)
        {
            var hood = Placed(parent, "Fume hood", at, yaw);
            const float depth = .85f, height = 2.35f;
            Solid(hood, "Fume hood collision", new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), enamel, false);
            Box(hood, "Base cabinet", new Vector3(0, .43f, 0), new Vector3(width, .86f, depth), enamel);
            Box(hood, "Hood worktop", new Vector3(0, .89f, .02f), new Vector3(width, .04f, depth - .02f), epoxy);
            for (int side = -1; side <= 1; side += 2)
                Box(hood, "Hood side wall", new Vector3(side * (width * .5f - .04f), 1.5f, 0), new Vector3(.08f, 1.2f, depth), enamel);
            Box(hood, "Hood back baffle", new Vector3(0, 1.5f, -depth * .5f + .05f), new Vector3(width - .1f, 1.2f, .03f), stainless);
            Box(hood, "Hood canopy", new Vector3(0, 2.2f, 0), new Vector3(width, .3f, depth), enamel);
            Box(hood, "Hood light lens", new Vector3(0, 2.04f, .05f), new Vector3(width - .25f, .02f, .2f), nightPanel);
            Box(hood, "Sash glass", new Vector3(0, 1.62f, depth * .5f - .03f), new Vector3(width - .14f, .76f, .012f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(hood, "Sash handle", new Vector3(0, 1.24f, depth * .5f), new Vector3(width - .2f, .03f, .04f), stainless);
            Box(hood, "Airflow monitor", new Vector3(width * .5f - .2f, 2.2f, depth * .5f + .002f), new Vector3(.16f, .08f, .006f), darkMetal);
            Box(hood, "Airflow good", new Vector3(width * .5f - .2f, 2.2f, depth * .5f + .006f), new Vector3(.1f, .02f, .004f), ledGreen);
            Cylinder(hood, "Exhaust duct", new Vector3(0, (height + Ceiling) * .5f, -.1f), .15f, Ceiling - height, steel);
            for (int i = 0; i < 2; i++)
            {
                var item = Placed(hood, "Hood glassware", new Vector3(-.3f + i * .55f, .91f, -.05f), 0);
                Glassware(item, dressing);
            }
        }

        static void InstrumentCabinet(Transform parent, Vector3 at, float yaw, float width, float height)
        {
            var cabinet = Placed(parent, "Glass instrument cabinet", at, yaw);
            const float depth = .55f;
            Solid(cabinet, "Cabinet carcass", new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), enamel);
            Box(cabinet, "Cabinet interior", new Vector3(0, height * .58f, depth * .5f + .003f), new Vector3(width - .1f, height * .62f, .004f), darkMetal);
            for (int s = 0; s < 3; s++)
            {
                float y = height * .32f + s * height * .2f;
                Box(cabinet, "Cabinet shelf", new Vector3(0, y, depth * .5f + .02f), new Vector3(width - .12f, .015f, .04f), stainless);
                for (int i = 0; i < 4; i++)
                    if (dressing.NextDouble() < .7)
                        Box(cabinet, "Stored instrument", new Vector3(-width * .35f + i * width * .23f, y + .08f, depth * .5f + .01f), new Vector3(.14f, .14f, .015f),
                            dressing.NextDouble() < .5 ? plasticGrey : enamel);
            }
            Box(cabinet, "Glass door", new Vector3(0, height * .58f, depth * .5f + .03f), new Vector3(width - .06f, height * .64f, .01f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(cabinet, "Lower doors", new Vector3(0, height * .13f, depth * .5f + .005f), new Vector3(width - .04f, height * .22f, .012f), enamelTeal);
            Box(cabinet, "Cabinet status", new Vector3(width * .38f, height * .94f, depth * .5f + .004f), new Vector3(.04f, .02f, .004f), ledBlue);
        }

        static void Freezer(Transform parent, Vector3 at, float yaw, bool glazed)
        {
            var unit = Placed(parent, glazed ? "Glazed specimen freezer" : "Ultra low freezer", at, yaw);
            const float width = .86f, depth = .9f, height = 1.98f;
            Solid(unit, "Freezer cabinet", new Vector3(0, height * .5f + .06f, 0), new Vector3(width, height, depth), enamel);
            Box(unit, "Freezer door", new Vector3(0, 1.0f, depth * .5f + .02f), new Vector3(width - .04f, 1.6f, .04f), glazed ? enamelTeal : enamel);
            if (glazed)
            {
                Box(unit, "Freezer window", new Vector3(0, 1.08f, depth * .5f + .043f), new Vector3(width - .22f, 1.2f, .006f), glassTint).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Box(unit, "Freezer interior glow", new Vector3(0, 1.08f, depth * .5f + .036f), new Vector3(width - .26f, 1.16f, .004f), coldInterior);
                for (int r = 0; r < 4; r++) Box(unit, "Sample rack", new Vector3(0, .62f + r * .3f, depth * .5f + .039f), new Vector3(width - .3f, .2f, .004f), steel);
            }
            Box(unit, "Freezer handle", new Vector3(width * .36f, 1.05f, depth * .5f + .06f), new Vector3(.04f, .48f, .05f), stainless);
            Box(unit, "Control panel", new Vector3(0, 1.92f, depth * .5f + .004f), new Vector3(.34f, .12f, .008f), darkMetal);
            Box(unit, "Temperature readout", new Vector3(-.05f, 1.92f, depth * .5f + .009f), new Vector3(.16f, .04f, .004f), ledBlue);
            Box(unit, "Alarm ok", new Vector3(.11f, 1.92f, depth * .5f + .009f), new Vector3(.025f, .025f, .004f), ledGreen);
            for (int i = 0; i < 5; i++) Box(unit, "Compressor louvre", new Vector3(0, .1f + i * .035f, depth * .5f + .003f), new Vector3(width - .2f, .012f, .006f), darkMetal);
            Box(unit, "Rear service panel", new Vector3(0, 1.05f, -depth * .5f - .004f), new Vector3(width - .1f, 1.7f, .008f), plasticGrey);
            for (int i = 0; i < 14; i++) Box(unit, "Condenser fin", new Vector3(0, .35f + i * .05f, -depth * .5f - .01f), new Vector3(width - .22f, .012f, .006f), darkMetal);
            Box(unit, "Rear power inlet", new Vector3(width * .3f, 1.55f, -depth * .5f - .012f), new Vector3(.1f, .07f, .01f), darkMetal);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Cylinder(unit, "Caster", new Vector3(x * .34f, .035f, z * .36f), .035f, .05f, rubber, new Vector3(0, 0, 90));
        }

        static void Dewar(Transform parent, Vector3 at)
        {
            var tank = Placed(parent, "Liquid nitrogen dewar", at, 0);
            Solid(tank, "Dewar body", new Vector3(0, .62f, 0), new Vector3(.5f, 1.04f, .5f), stainless, false);
            Cylinder(tank, "Dewar shell", new Vector3(0, .62f, 0), .25f, 1.04f, stainless);
            Cylinder(tank, "Dewar shoulder", new Vector3(0, 1.17f, 0), .19f, .08f, stainless);
            Cylinder(tank, "Dewar neck", new Vector3(0, 1.26f, 0), .07f, .12f, steel);
            Cylinder(tank, "Pressure gauge", new Vector3(.1f, 1.3f, .06f), .035f, .015f, paper, new Vector3(90, 0, 0));
            Cylinder(tank, "Dolly base", new Vector3(0, .07f, 0), .29f, .05f, darkMetal);
            Cylinder(tank, "Hazard band", new Vector3(0, .9f, 0), .252f, .05f, hazardStripe);
        }

        static void ServerRack(Transform parent, Vector3 at, float yaw, int seed)
        {
            var random = new System.Random(seed);
            var rack = Placed(parent, "Research compute rack", at, yaw);
            const float width = .62f, depth = 1.0f, height = 2.1f;
            Solid(rack, "Rack cabinet", new Vector3(0, height * .5f, 0), new Vector3(width, height, depth), darkMetal);
            Box(rack, "Rack top", new Vector3(0, height + .01f, 0), new Vector3(width + .01f, .02f, depth + .01f), steel);
            var leds = new List<Renderer>();
            for (int face = -1; face <= 1; face += 2)
            {
                float z = face * (depth * .5f + .005f);
                Box(rack, "Perforated door frame", new Vector3(0, height * .5f, z), new Vector3(width - .02f, height - .04f, .01f), rubber);
                for (int u = 0; u < 14; u++)
                {
                    float y = .2f + u * .13f;
                    Box(rack, "Server unit", new Vector3(0, y, z + face * .004f), new Vector3(width - .1f, .1f, .004f), plasticGrey);
                    for (int l = 0; l < 3; l++)
                    {
                        double roll = random.NextDouble();
                        var led = Box(rack, "Activity light", new Vector3(width * .3f - l * .035f, y, z + face * .008f), new Vector3(.012f, .012f, .004f),
                            roll < .55 ? ledGreen : roll < .85 ? ledBlue : ledAmber);
                        if (random.NextDouble() < .5) leds.Add(led.GetComponent<Renderer>());
                    }
                }
            }
            var blinker = rack.gameObject.AddComponent<StatusBlinker>();
            blinker.lights = leds.ToArray(); blinker.seed = seed;
        }

        static void Pipe(Transform parent, Vector3 from, Vector3 to, float radius, Material material)
        {
            Vector3 mid = (from + to) * .5f, delta = to - from;
            var pipe = Primitive(parent, "Service pipe", PrimitiveType.Cylinder, mid, new Vector3(radius * 2, delta.magnitude * .5f, radius * 2), material, false);
            pipe.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            pipe.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static void WallSign(Transform parent, Vector3 at, float yaw, Material face, float size)
        {
            var sign = Placed(parent, "Pictogram sign", at, yaw);
            Box(sign, "Sign backing", Vector3.zero, new Vector3(size + .03f, size + .03f, .015f), darkMetal);
            Box(sign, "Sign face", new Vector3(0, 0, .009f), new Vector3(size, size, .003f), face);
        }

        static void Extinguisher(Transform parent, Vector3 at, float yaw)
        {
            var unit = Placed(parent, "Fire extinguisher", at, yaw);
            Box(unit, "Wall bracket", new Vector3(0, .1f, -.08f), new Vector3(.1f, .3f, .02f), darkMetal);
            Cylinder(unit, "Extinguisher body", new Vector3(0, 0, 0), .075f, .5f, extinguisherRed);
            Cylinder(unit, "Valve", new Vector3(0, .29f, 0), .025f, .08f, darkMetal);
            Box(unit, "Handle", new Vector3(0, .34f, .03f), new Vector3(.03f, .02f, .12f), darkMetal);
            Cylinder(unit, "Hose", new Vector3(.07f, .1f, .03f), .012f, .34f, rubber);
        }

        static void WallClock(Transform parent, Vector3 at, float yaw)
        {
            var clock = Placed(parent, "Wall clock", at, yaw);
            Cylinder(clock, "Clock rim", Vector3.zero, .16f, .05f, darkMetal, new Vector3(90, 0, 0));
            Cylinder(clock, "Clock face", new Vector3(0, 0, .026f), .145f, .004f, paper, new Vector3(90, 0, 0));
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30 * Mathf.Deg2Rad;
                Box(clock, "Hour mark", new Vector3(Mathf.Sin(a) * .12f, Mathf.Cos(a) * .12f, .03f), new Vector3(.008f, i % 3 == 0 ? .03f : .015f, .003f), darkMetal)
                    .transform.localRotation = Quaternion.Euler(0, 0, -i * 30);
            }
            Box(clock, "Hour hand", new Vector3(.03f, .02f, .033f), new Vector3(.012f, .08f, .003f), darkMetal).transform.localRotation = Quaternion.Euler(0, 0, -62);
            Box(clock, "Minute hand", new Vector3(-.04f, .03f, .035f), new Vector3(.008f, .12f, .003f), darkMetal).transform.localRotation = Quaternion.Euler(0, 0, 52);
        }

        static void Dispenser(Transform parent, Vector3 at, float yaw)
        {
            var unit = Placed(parent, "Sanitiser dispenser", at, yaw);
            Box(unit, "Dispenser body", Vector3.zero, new Vector3(.12f, .22f, .09f), enamel);
            Box(unit, "Dispenser window", new Vector3(0, .02f, .046f), new Vector3(.06f, .08f, .003f), accentTeal);
            Box(unit, "Drip tray", new Vector3(0, -.13f, .02f), new Vector3(.1f, .02f, .07f), plasticGrey);
        }

        static void CardReader(Transform parent, Vector3 at, float yaw, Material state)
        {
            var reader = Placed(parent, "Access card reader", at, yaw);
            Box(reader, "Reader body", Vector3.zero, new Vector3(.09f, .14f, .03f), darkMetal);
            Box(reader, "Reader pad", new Vector3(0, -.02f, .016f), new Vector3(.06f, .06f, .003f), plasticGrey);
            Box(reader, "Reader state", new Vector3(0, .05f, .017f), new Vector3(.04f, .012f, .003f), state);
        }
    }
}
