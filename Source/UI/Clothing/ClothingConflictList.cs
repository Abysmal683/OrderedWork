using OrderedWork.Core;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Clothing
{
    internal static class ClothingConflictList
    {
        public static void Draw(Rect rect, ClothingResolution resolution)
        {
            float y = rect.y;
            if (resolution.Conflicts.Count == 0)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, 20f), "No conflicts.");
                return;
            }
            foreach (ClothingConflict conflict in resolution.Conflicts)
            {
                Widgets.LabelEllipses(new Rect(rect.x, y, rect.width, 20f),
                    Reason(conflict.Reason) + " · " + (conflict.Candidate ?? "rule"));
                y += 20f;
            }
        }

        private static string Reason(ClothingConflictReason reason)
        {
            switch (reason)
            {
                case ClothingConflictReason.PolicyDenied: return "policy denied";
                case ClothingConflictReason.PolicyContradiction: return "policy contradiction";
                case ClothingConflictReason.NoCandidates: return "no candidates";
                case ClothingConflictReason.SlotOccupied: return "slot occupied";
                default: return "invalid rule";
            }
        }
    }
}