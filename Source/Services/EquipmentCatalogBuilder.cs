using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Core;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    internal sealed class EquipmentCatalogSnapshot
    {
        public Dictionary<string, ApparelFootprint> Footprints { get; } =
            new Dictionary<string, ApparelFootprint>(StringComparer.Ordinal);
        public HashSet<string> AllowedApparel { get; } =
            new HashSet<string>(StringComparer.Ordinal);
        public List<ApparelSlotKey> Cells { get; } = new List<ApparelSlotKey>();
    }

    /// <summary>
    /// Read-only RimWorld adapter for the dynamic apparel cell catalog.
    /// </summary>
    internal static class EquipmentCatalogBuilder
    {
        public static EquipmentCatalogSnapshot Build(Pawn? pawn)
        {
            EquipmentCatalogSnapshot snapshot = new EquipmentCatalogSnapshot();
            BodyDef body = pawn?.RaceProps?.body ?? BodyDefOf.Human;
            ApparelPolicy? policy = pawn?.outfits?.CurrentApparelPolicy;
            HashSet<ApparelSlotKey> cells = new HashSet<ApparelSlotKey>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null || !def.IsApparel || def.apparel == null)
                {
                    continue;
                }

                if (policy != null && policy.filter != null && !policy.filter.Allows(def))
                {
                    continue;
                }

                ApparelFootprint footprint = new ApparelFootprint { DefName = def.defName };
                foreach (BodyPartGroupDef group in def.apparel.bodyPartGroups ?? new List<BodyPartGroupDef>())
                {
                    foreach (ApparelLayerDef layer in def.apparel.layers ?? new List<ApparelLayerDef>())
                    {
                        ApparelSlotKey cell = new ApparelSlotKey(body.defName, group.defName, layer.defName);
                        if (cell.IsValid)
                        {
                            footprint.Cells.Add(cell);
                            cells.Add(cell);
                        }
                    }
                }

                if (footprint.Cells.Count == 0)
                {
                    continue;
                }

                footprint.Normalize();
                snapshot.Footprints[footprint.DefName] = footprint;
                snapshot.AllowedApparel.Add(def.defName);
            }

            snapshot.Cells.AddRange(cells
                .OrderBy(cell => cell.BodyPartGroupDefName, StringComparer.Ordinal)
                .ThenBy(cell => cell.ApparelLayerDefName, StringComparer.Ordinal));
            return snapshot;
        }
    }
}
