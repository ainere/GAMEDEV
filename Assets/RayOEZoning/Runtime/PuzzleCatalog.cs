using System;

namespace RayOEZoning
{
    [Serializable]
    public sealed class ClueDefinition
    {
        public string id;
        public string title;
        public string digit;
        public string relation;

        public ClueDefinition(string id, string title, string digit, string relation)
        {
            this.id = id;
            this.title = title;
            this.digit = digit;
            this.relation = relation;
        }
    }

    public static class PuzzleCatalog
    {
        public static readonly ClueDefinition[] Clues =
        {
            new ClueDefinition("Eye", "Eye", "2", "The Eye is first."),
            new ClueDefinition("Moon", "Moon", "6", string.Empty),
            new ClueDefinition("Gate", "Gate", "4", "The Gate is not last."),
            new ClueDefinition("Crown", "Crown", "9", "The Crown comes immediately after the Moon.")
        };

        public const string Code = "2469";
    }
}
