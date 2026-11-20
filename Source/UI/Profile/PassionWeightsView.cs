#nullable disable
using System.Collections.Generic;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Renders the full global passion-weight table. The settings tab owns scrolling;
    /// this view has no internal row cap or scrollbar.
    /// </summary>
    internal static class PassionWeightsView
    {
        private const float PassionRowHeight = 26f;

        public static void Draw(Listing_Standard list, OrderedWorkSettings settings, bool drawHeader = true)
        {
            if (drawHeader)
            {
                list.Label(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.Title", "Recommendation passion weights (global). Level 0 / per level / min level:"));
            }
            PassionWeightPolicy.EnsureDefaults(settings);
            List<PassionSource.PassionInfo> registered = PassionSource.Registered;
            Rect area = list.GetRect(registered.Count * PassionRowHeight);
            for (int i = 0; i < registered.Count; i++)
            {
                DrawRow(area, i, registered[i], settings);
            }

            bool canReset = PassionWeightPolicy.HasChangesFromDefaults(settings);
            if (UiButtonBar.Button(list.GetRect(28f), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.Reset", "Reset recommendation weights"), canReset,
                canReset
                    ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.ResetTip", "Restore all passion weights to their defaults.")
                    : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.DefaultsMatch", "Weights already match the current defaults.")))
            {
                PassionWeightPolicy.ResetToDefaults(settings);
                Messages.Message(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.ResetDone", "Recommendation weights reset."), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private static void DrawRow(
            Rect area,
            int index,
            PassionSource.PassionInfo info,
            OrderedWorkSettings settings)
        {
            PassionWeightEntry entry = PassionWeightPolicy.Find(settings, info.Passion);
            if (entry == null)
            {
                return;
            }

            Rect row = new Rect(area.x, area.y + index * PassionRowHeight, area.width, 24f);
            UiZebra.Draw(row, index);
            WorkTypePaletteView.PassionVisual visual = WorkTypePaletteView.ResolvePassionVisual(info.Passion);
            if (visual.Texture != null)
            {
                Color previous = GUI.color;
                GUI.color = visual.Tint;
                GUI.DrawTexture(new Rect(row.x, row.y + 5f, 16f, 16f), visual.Texture);
                GUI.color = previous;
            }

            Widgets.Label(new Rect(row.x + 20f, row.y, 140f, 24f), info.Label);
            string description = PassionSource.DescriptionOf(info.Passion);
            float learnRate = PassionSource.LearnRateOf(info.Passion);
            if (learnRate > 0f)
            {
                string rateLine = OrderedWork.Localization.OrderedWorkText.T("OrderedWork.PassionWeights.LearnRate", "Learn rate: x") + learnRate.ToString("0.##");
                description = string.IsNullOrEmpty(description)
                    ? rateLine
                    : description + "\n\n" + rateLine;
            }
            if (!string.IsNullOrEmpty(description))
            {
                TooltipHandler.TipRegion(row, description);
            }

            float fieldWidth = Mathf.Max(54f, (row.width - 170f - 24f) / 3f);
            Widgets.TextFieldNumeric(new Rect(row.x + 174f, row.y, fieldWidth, 24f),
                ref entry.level0Score, ref entry.level0Buffer, -20, 20);
            Widgets.TextFieldNumeric(new Rect(row.x + 178f + fieldWidth, row.y, fieldWidth, 24f),
                ref entry.perLevelBonus, ref entry.perLevelBuffer, -20, 20);
            Widgets.TextFieldNumeric(new Rect(row.x + 182f + fieldWidth * 2f, row.y, fieldWidth, 24f),
                ref entry.minLevel, ref entry.minLevelBuffer, 0, 20);
        }
    }
}