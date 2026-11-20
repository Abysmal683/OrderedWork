using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Data;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI.Clothing
{
    internal static class ClothingPreviewPanel
    {
        private sealed class PreviewData
        {
            public Pawn? Pawn;
            public string PresetId = "";
            public WorkTypeDef? WorkType;
            public int Tick;
            public ClothingPreset? Preset;
            public ClothingContext? Context;
            public ClothingResolution? Resolution;
            public bool Assigned;
        }

        private static PreviewData? cached;

        public static void Draw(Rect rect, Pawn? pawn, PawnWorkOrder? entry)
        {
            PreviewData data = GetPreview(pawn, entry);
            ApparelPolicy? policy = pawn?.outfits?.CurrentApparelPolicy;
            string policyLabel = policy == null
                ? "none"
                : (string.IsNullOrEmpty(policy.RenamableLabel) ? policy.BaseLabel : policy.RenamableLabel);

            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f),
                "Clothing preview · " + (data.WorkType?.labelShort ?? "no active WorkType"));
            Widgets.Label(new Rect(rect.x, rect.y + 22f, rect.width, 20f),
                "Policy: " + policyLabel);
            Widgets.Label(new Rect(rect.x, rect.y + 44f, rect.width, 20f),
                "Preset: " + (data.Preset?.Name ?? "none") + (data.Assigned ? " (assigned)" : " (preview fallback)"));

            if (data.Preset == null || data.Context == null || data.Resolution == null)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 72f, rect.width, 20f),
                    "No clothing preset. Create one in Mod Settings.");
                return;
            }

            float gridY = rect.y + 70f;
            float gridHeight = ClothingSlotGrid.Height(data.Context.KnownSlots.Count);
            ClothingSlotGrid.Draw(new Rect(rect.x, gridY, rect.width, gridHeight), data.Context.KnownSlots, data.Resolution);

            float listY = gridY + gridHeight + 8f;
            ClothingSelectionList.Draw(new Rect(rect.x, listY, rect.width, 100f), data.Resolution);
            ClothingConflictList.Draw(new Rect(rect.x, listY + 110f, rect.width, 80f), data.Resolution);
        }

        public static float Height()
        {
            return 620f;
        }

        private static PreviewData GetPreview(Pawn? pawn, PawnWorkOrder? entry)
        {
            List<ClothingPreset> presets = ClothingPresetStore.LoadAll();
            string assignedId = entry?.clothingPresetId ?? "";
            ClothingPreset? preset = presets.FirstOrDefault(value => value.Id == assignedId)
                ?? presets.FirstOrDefault();
            WorkTypeDef? activeWorkType = ActiveWorkService.GetActiveWorkType(pawn);
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            string presetId = preset?.Id ?? "";

            if (cached != null
                && ReferenceEquals(cached.Pawn, pawn)
                && cached.PresetId == presetId
                && ReferenceEquals(cached.WorkType, activeWorkType)
                && tick - cached.Tick < 60)
            {
                return cached;
            }

            PreviewData data = new PreviewData
            {
                Pawn = pawn,
                PresetId = presetId,
                WorkType = activeWorkType,
                Tick = tick,
                Preset = preset,
                Assigned = preset != null && preset.Id == assignedId
            };
            if (preset != null)
            {
                data.Context = ClothingContextBuilder.Build(pawn, preset);
                data.Resolution = ClothingResolver.Resolve(preset, data.Context);
            }
            cached = data;
            return data;
        }
    }
}