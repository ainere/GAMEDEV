using System;
using System.Collections.Generic;

namespace RayOEZoning
{
    public enum ClueKind
    {
        Code,
        Order
    }

    // One readable record: a symbol and its digit, or ordering statements about the symbols.
    [Serializable]
    public sealed class ClueDefinition
    {
        public string id;
        public ClueKind kind;
        public string symbol;
        public string digit;
        public string[] statements;

        public string Title => kind == ClueKind.Code ? symbol : "Order";

        public static ClueDefinition Code(string symbol, string digit) =>
            new ClueDefinition { id = symbol, kind = ClueKind.Code, symbol = symbol, digit = digit, statements = new string[0] };

        public static ClueDefinition Order(string id, params string[] statements) =>
            new ClueDefinition { id = id, kind = ClueKind.Order, symbol = string.Empty, digit = string.Empty, statements = statements };
    }

    // A whole run: four symbol digits and the two order notes that sort them into the keypad code.
    public sealed class PuzzleRun
    {
        public readonly int seed;
        public readonly string code;
        // Symbols in keypad order.
        public readonly string[] order;
        // Code fragments first, then order notes.
        public readonly ClueDefinition[] clues;

        public PuzzleRun(int seed, string code, string[] order, ClueDefinition[] clues)
        {
            this.seed = seed;
            this.code = code;
            this.order = order;
            this.clues = clues;
        }
    }

    public static class PuzzleCatalog
    {
        public static readonly string[] Symbols =
        {
            "Eye", "Moon", "Gate", "Crown", "Star", "Sun", "Key", "Bell",
            "Fish", "Leaf", "Bolt", "Anchor", "Drop", "Spiral", "Flame", "Tree"
        };

        static readonly string[] Places = { "FIRST", "SECOND", "THIRD", "LAST" };

        // The fixed puzzle used when no run has been rolled, so every check sees the same code.
        public static readonly PuzzleRun Classic = new PuzzleRun(0, "2469", new[] { "Eye", "Gate", "Moon", "Crown" }, new[]
        {
            ClueDefinition.Code("Eye", "2"),
            ClueDefinition.Code("Moon", "6"),
            ClueDefinition.Code("Gate", "4"),
            ClueDefinition.Code("Crown", "9"),
            ClueDefinition.Order("Order A", "EYE IS FIRST", "GATE IS NOT LAST"),
            ClueDefinition.Order("Order B", "CROWN RIGHT AFTER MOON")
        });

        enum Rule { At, NotAt, RightAfter, Before }

        struct Statement
        {
            public Rule rule;
            public int a, b, place;

            public bool Holds(int[] position)
            {
                switch (rule)
                {
                    case Rule.At: return position[a] == place;
                    case Rule.NotAt: return position[a] != place;
                    case Rule.RightAfter: return position[b] == position[a] + 1;
                    default: return position[a] < position[b];
                }
            }

            public string Text(string[] names)
            {
                string first = names[a].ToUpperInvariant();
                switch (rule)
                {
                    case Rule.At: return first + " IS " + Places[place];
                    case Rule.NotAt: return first + " IS NOT " + Places[place];
                    case Rule.RightAfter: return names[b].ToUpperInvariant() + " RIGHT AFTER " + first;
                    default: return first + " BEFORE " + names[b].ToUpperInvariant();
                }
            }
        }

        // Builds a fresh run. The order notes together have exactly one solution; neither note alone does.
        public static PuzzleRun Generate(int seed)
        {
            var random = new Random(seed);
            var pool = new List<string>(Symbols);
            Shuffle(pool, random);
            var order = pool.GetRange(0, 4).ToArray();
            var digits = new string[4];
            for (int i = 0; i < 4; i++) digits[i] = random.Next(10).ToString();
            if (digits[0] == digits[1] && digits[1] == digits[2] && digits[2] == digits[3])
                digits[3] = ((digits[3][0] - '0' + 1 + random.Next(9)) % 10).ToString();

            var statements = Statements(random);
            int split = statements.Count == 4 ? 2 : statements.Count - 1;
            var first = new List<string>(); var second = new List<string>();
            for (int i = 0; i < statements.Count; i++)
                (i < split ? first : second).Add(statements[i].Text(order));

            var fragments = new List<ClueDefinition>();
            for (int i = 0; i < 4; i++) fragments.Add(ClueDefinition.Code(order[i], digits[i]));
            Shuffle(fragments, random);
            fragments.Add(ClueDefinition.Order("Order A", first.ToArray()));
            fragments.Add(ClueDefinition.Order("Order B", second.ToArray()));
            return new PuzzleRun(seed, string.Concat(digits), order, fragments.ToArray());
        }

        // Counts the symbol orders that satisfy every statement, to confirm a run has one answer.
        public static int Solutions(PuzzleRun run)
        {
            int count = 0;
            foreach (var position in Permutations())
            {
                var names = new string[4];
                for (int i = 0; i < 4; i++) names[position[i]] = run.order[i];
                bool valid = true;
                foreach (var clue in run.clues)
                    if (clue.kind == ClueKind.Order)
                        foreach (string text in clue.statements)
                            valid &= Reads(text, names);
                if (valid) count++;
            }
            return count;
        }

        static bool Reads(string text, string[] sequence)
        {
            int Index(string word) => Array.FindIndex(sequence, s => s.ToUpperInvariant() == word);
            var words = text.Split(' ');
            if (words.Length == 3 && words[1] == "BEFORE") return Index(words[0]) < Index(words[2]);
            if (words.Length == 4 && words[1] == "RIGHT") return Index(words[0]) == Index(words[3]) + 1;
            if (words.Length == 3) return Index(words[0]) == Array.IndexOf(Places, words[2]);
            return Index(words[0]) != Array.IndexOf(Places, words[3]);
        }

        static List<Statement> Statements(Random random)
        {
            // Index i is the symbol at keypad position i.
            var candidates = new List<Statement>();
            for (int i = 0; i < 4; i++)
            {
                candidates.Add(new Statement { rule = Rule.At, a = i, place = i });
                if (i != 0) candidates.Add(new Statement { rule = Rule.NotAt, a = i, place = 0 });
                if (i != 3) candidates.Add(new Statement { rule = Rule.NotAt, a = i, place = 3 });
                for (int j = i + 1; j < 4; j++)
                    candidates.Add(new Statement { rule = j == i + 1 && random.NextDouble() < .6 ? Rule.RightAfter : Rule.Before, a = i, b = j });
            }
            var permutations = Permutations();
            List<Statement> best = null;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                Shuffle(candidates, random);
                var chosen = new List<Statement>();
                bool placed = false;
                int remaining = permutations.Count;
                foreach (var candidate in candidates)
                {
                    if (candidate.rule == Rule.At && placed) continue;
                    chosen.Add(candidate);
                    int count = Count(permutations, chosen);
                    if (count < remaining) { remaining = count; placed |= candidate.rule == Rule.At; }
                    else chosen.RemoveAt(chosen.Count - 1);
                    if (remaining == 1) break;
                }
                for (int i = chosen.Count - 1; i >= 0 && chosen.Count > 2; i--)
                {
                    var removed = chosen[i];
                    chosen.RemoveAt(i);
                    if (Count(permutations, chosen) != 1) chosen.Insert(i, removed);
                }
                if (best == null || Score(chosen) > Score(best)) best = chosen;
                if (chosen.Count == 3) break;
            }
            return best;
        }

        static int Score(List<Statement> statements) => statements.Count == 3 ? 3 : statements.Count == 4 ? 2 : 1;

        static int Count(List<int[]> permutations, List<Statement> statements)
        {
            int count = 0;
            foreach (var position in permutations)
            {
                bool valid = true;
                foreach (var statement in statements) valid &= statement.Holds(position);
                if (valid) count++;
            }
            return count;
        }

        // Every assignment of the four symbols to keypad positions.
        static List<int[]> Permutations()
        {
            var result = new List<int[]>();
            for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
            for (int c = 0; c < 4; c++)
            for (int d = 0; d < 4; d++)
            {
                var position = new[] { a, b, c, d };
                if (a != b && a != c && a != d && b != c && b != d && c != d) result.Add(position);
            }
            return result;
        }

        static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
