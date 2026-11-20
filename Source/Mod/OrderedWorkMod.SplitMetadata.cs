using OrderedWork.Config;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
                private void ResetSplitMetadataState()
        {
            splitMerge.MetaDefName = "";
            splitMerge.MetaLabel = "";
            splitMerge.MetaDescription = "";
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

            if (splitMerge.MetaDefName != selectedGiverType)
            {
                splitMerge.MetaDefName = selectedGiverType;
                splitMerge.MetaLabel = split.Label ?? "";
                splitMerge.MetaDescription = split.Description ?? "";
            }

            list.Gap(10f);
            list.Label(ConfigText("OrderedWork.Config.SplitMeta.Info", "Split metadata (defName is stable):"));
            list.Label(string.Format(ConfigText("OrderedWork.Config.SplitMeta.DefName", "defName: {0}"), selectedGiverType));
            Rect labelRow = list.GetRect(26f);
            Widgets.Label(new Rect(labelRow.x, labelRow.y, 110f, 24f), ConfigText("OrderedWork.Config.SplitMeta.Label", "Label"));
            splitMerge.MetaLabel = Widgets.TextField(
                new Rect(labelRow.x + 114f, labelRow.y, labelRow.width - 114f, 24f), splitMerge.MetaLabel ?? "");
            Rect descRow = list.GetRect(26f);
            Widgets.Label(new Rect(descRow.x, descRow.y, 110f, 24f), ConfigText("OrderedWork.Config.SplitMeta.Description", "Description"));
            splitMerge.MetaDescription = Widgets.TextField(
                new Rect(descRow.x + 114f, descRow.y, descRow.width - 114f, 24f), splitMerge.MetaDescription ?? "");

            bool changed = splitMerge.MetaLabel != (split.Label ?? "")
                || splitMerge.MetaDescription != (split.Description ?? "");
            if (UiButtonBar.Button(list.GetRect(26f), ConfigText("OrderedWork.Config.SplitMeta.Save", "Save split metadata"), changed))
            {
                WorkConfigProfile editing = LoadEditingProfile();
                if (editing != null && editing.WorkTypes.TryGetValue(selectedGiverType, out WorkTypeConfig target)
                    && target != null)
                {
                    target.Label = splitMerge.MetaLabel.Trim();
                    target.Description = splitMerge.MetaDescription.Trim();
                    WorkConfigStore.SaveProfile(editing, true);
                    WorkConfigStore.SavePending(editing);
                    OrderedWorkStartup.ApplyProfileNow(editing, Settings, this);
                    Messages.Message(string.Format(ConfigText("OrderedWork.Config.SplitMeta.Saved", "Split metadata saved for {0}."), selectedGiverType),
                        MessageTypeDefOf.TaskCompletion, false);
                }
            }
        }
    }
}