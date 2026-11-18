using OrderedWork.Config;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private string splitMetaDefName = "";
        private string splitMetaLabel = "";
        private string splitMetaDescription = "";

        private void ResetSplitMetadataState()
        {
            splitMetaDefName = "";
            splitMetaLabel = "";
            splitMetaDescription = "";
        }

        private void DrawSplitMetadata(Listing_Standard list)
        {
            WorkConfigProfile profile = LoadEditingProfile();
            if (profile == null || string.IsNullOrEmpty(selectedGiverType)
                || !profile.WorkTypes.TryGetValue(selectedGiverType, out WorkTypeConfig split)
                || split == null || split.Vanilla || string.IsNullOrEmpty(split.Base))
            {
                return;
            }

            if (splitMetaDefName != selectedGiverType)
            {
                splitMetaDefName = selectedGiverType;
                splitMetaLabel = split.Label ?? "";
                splitMetaDescription = split.Description ?? "";
            }

            list.Gap(10f);
            list.Label("Split metadata (defName is stable):");
            list.Label("defName: " + selectedGiverType);
            Rect labelRow = list.GetRect(26f);
            Widgets.Label(new Rect(labelRow.x, labelRow.y, 110f, 24f), "Label");
            splitMetaLabel = Widgets.TextField(
                new Rect(labelRow.x + 114f, labelRow.y, labelRow.width - 114f, 24f), splitMetaLabel ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), "Description");
            splitMetaDescription = Widgets.TextField(
                new Rect(descRow.x + 114f, descRow.y, descRow.width - 114f, 24f), splitMetaDescription ?? "");

            bool changed = splitMetaLabel != (split.Label ?? "")
                || splitMetaDescription != (split.Description ?? "");
            if (DrawActionButton(list.GetRect(26f), "Save split metadata", changed))
            {
                WorkConfigProfile editing = LoadEditingProfile();
                if (editing != null && editing.WorkTypes.TryGetValue(selectedGiverType, out WorkTypeConfig target)
                    && target != null)
                {
                    target.Label = splitMetaLabel.Trim();
                    target.Description = splitMetaDescription.Trim();
                    WorkConfigStore.SaveProfile(editing, true);
                    WorkConfigStore.SavePending(editing);
                    OrderedWorkStartup.ApplyProfileNow(editing, Settings, this);
                    Messages.Message("Split metadata saved for " + selectedGiverType + ".",
                        MessageTypeDefOf.TaskCompletion, false);
                }
            }
        }
    }
}