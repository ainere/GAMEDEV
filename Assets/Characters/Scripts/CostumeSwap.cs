using System;
using UnityEngine;

namespace ShowcaseCharacters
{
    /// <summary>Switches a figure between two outfits by swapping materials and toggling outfit-only parts.</summary>
    public sealed class CostumeSwap : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public Renderer renderer;
            public Material outfitA;
            public Material outfitB;
        }

        [SerializeField] Slot[] slots = Array.Empty<Slot>();
        [SerializeField] GameObject[] outfitAOnly = Array.Empty<GameObject>();
        [SerializeField] GameObject[] outfitBOnly = Array.Empty<GameObject>();

        public bool WearingB { get; private set; }

        public void Configure(Slot[] newSlots, GameObject[] aOnly, GameObject[] bOnly)
        {
            slots = newSlots;
            outfitAOnly = aOnly;
            outfitBOnly = bOnly;
        }

        public void Wear(bool outfitB)
        {
            WearingB = outfitB;
            foreach (Slot slot in slots)
            {
                if (slot.renderer != null)
                    slot.renderer.sharedMaterial = outfitB ? slot.outfitB : slot.outfitA;
            }
            foreach (GameObject go in outfitAOnly)
                if (go != null) go.SetActive(!outfitB);
            foreach (GameObject go in outfitBOnly)
                if (go != null) go.SetActive(outfitB);
        }
    }
}
