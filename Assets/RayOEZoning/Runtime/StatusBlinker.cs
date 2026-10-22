using UnityEngine;

namespace RayOEZoning
{
    // Irregular equipment activity lights. Purely cosmetic and independent of game state.
    public sealed class StatusBlinker : MonoBehaviour
    {
        public Renderer[] lights;
        public int seed;
        float[] rates, offsets;

        void Start()
        {
            if (lights == null) return;
            var random = new System.Random(seed);
            rates = new float[lights.Length]; offsets = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                rates[i] = .6f + (float)random.NextDouble() * 5f;
                offsets[i] = (float)random.NextDouble() * 10f;
            }
        }

        void Update()
        {
            if (rates == null) return;
            float time = Time.time;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i]) lights[i].enabled = Mathf.Sin((time + offsets[i]) * rates[i]) + Mathf.Sin((time + offsets[i]) * rates[i] * 2.7f) > -.4f;
        }
    }
}
