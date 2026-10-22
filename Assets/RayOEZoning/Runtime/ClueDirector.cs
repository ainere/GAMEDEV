using System;
using System.Collections.Generic;
using UnityEngine;

namespace RayOEZoning
{
    // Rolls a new code and clue arrangement for each run and writes it onto the chosen surfaces.
    [DefaultExecutionOrder(900)]
    public sealed class ClueDirector : MonoBehaviour
    {
        public StealthGame game;
        public ClueSpot[] spots;
        public bool randomizeOnPlay = true;
        // Seed of the arrangement currently written into the scene.
        public int seed = 1;

        public PuzzleRun Run { get; private set; }

        void Awake()
        {
            Apply(randomizeOnPlay ? NewSeed() : seed);
        }

        public void Reroll() => Apply(NewSeed());

        public static int NewSeed()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--puzzle-seed");
            if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int fixedSeed)) return fixedSeed;
            return (Environment.TickCount ^ Guid.NewGuid().GetHashCode()) & int.MaxValue;
        }

        public void Apply(int value)
        {
            seed = value;
            Run = PuzzleCatalog.Generate(value);
            var random = new System.Random(value * 7919 + 17);
            foreach (var spot in spots) if (spot) spot.Clear();

            var free = new List<ClueSpot>();
            foreach (var spot in spots) if (spot) free.Add(spot);
            var chosen = new List<(ClueSpot spot, int index)>();
            // One digit per zone, so every room on the route matters.
            for (int zone = 0, fragment = 0; zone < 4; zone++)
            {
                var options = free.FindAll(s => s.zone == zone);
                if (options.Count == 0) continue;
                var spot = options[random.Next(options.Count)];
                free.Remove(spot);
                chosen.Add((spot, fragment++));
            }
            // The two order notes go to two different rooms, on surfaces with room for sentences.
            var zones = new List<int> { 0, 1, 2, 3 };
            for (int note = 0; note < 2; note++)
            {
                ClueSpot pick = null;
                while (zones.Count > 0 && !pick)
                {
                    int zone = zones[random.Next(zones.Count)];
                    zones.Remove(zone);
                    var options = free.FindAll(s => s.zone == zone && s.allowOrder);
                    if (options.Count > 0) pick = options[random.Next(options.Count)];
                }
                if (!pick)
                {
                    var options = free.FindAll(s => s.allowOrder);
                    if (options.Count == 0) break;
                    pick = options[random.Next(options.Count)];
                }
                free.Remove(pick);
                chosen.Add((pick, 4 + note));
            }

            var clues = new List<HiddenClue>();
            foreach (var (spot, index) in chosen)
            {
                if (index >= Run.clues.Length) continue;
                spot.Show(Run.clues[index], index, new System.Random(random.Next()));
                if (spot.clue) clues.Add(spot.clue);
            }
            if (game) game.SetPuzzle(Run, clues.ToArray());
        }
    }
}
