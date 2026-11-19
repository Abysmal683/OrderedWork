using System.Collections.Generic;
using System.Linq;
using OrderedWork.Data;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    public partial class Dialog_WorkControlDetails
    {
        private void DrawClothingTab(Rect content)
        {
            PawnWorkOrder entry = OrderedWorkGameComponent.GetOrCreate()?.GetEntry(pawn);
            List<Apparel> worn = pawn.apparel?.WornApparel?.Where(a => a != null).ToList() ?? new List<Apparel>();
            float height = 360f + (worn.Count * 22f) + ((entry != null ? entry.clothingOverrides.Count : 0) * 22f);
            Rect view = new Rect(0f, 0f,
                Mathf.Max(content.width, 620f),
                Mathf.Max(content.height, height));

            Widgets.BeginScrollView(content, ref detailTabScroll, view);
            try
            {
                UiRows rows = new UiRows(view, 20f, 4f);
                ApparelPolicy policy = pawn.outfits?.CurrentApparelPolicy;
                string policyLabel = policy != null
                    ? (string.IsNullOrEmpty(policy.RenamableLabel) ? policy.BaseLabel : policy.RenamableLabel)
                    : "None";

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Clothing.Vanilla", "Vanilla apparel policy"));
                Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Clothing.Current", "Current: ") + policyLabel);

                DrawSectionTitle(rows, "Worn apparel (" + worn.Count + ")");
                if (worn.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    int rowIndex = 0;
                    foreach (Apparel apparel in worn)
                    {
                        DrawRow(rows, apparel.Label, rowIndex++);
                    }
                }

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Clothing.Policy", "Ordered Work clothing policy"));
                string preset = entry == null || string.IsNullOrEmpty(entry.clothingPresetId)
                    ? "not configured"
                    : entry.clothingPresetId;
                Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Clothing.Preset", "Preset: ") + preset);

                DrawSectionTitle(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.Clothing", "Clothing overrides"));
                if (entry == null || entry.clothingOverrides.Count == 0)
                {
                    Widgets.Label(rows.Next(), OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.None", "None."));
                }
                else
                {
                    int rowIndex = 0;
                    foreach (PawnClothingOverride value in entry.clothingOverrides)
                    {
                        DrawRow(rows, OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.WorkType", "WorkType ") + WorkTypeLabel(value.workTypeDefName)
                            + OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Overrides.ApparelPolicy", " | apparel policy ") + (string.IsNullOrEmpty(value.apparelPolicyId) ? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Palette.None", "none") : value.apparelPolicyId)
                            + " | " + value.source, rowIndex++);
                    }
                }

                Widgets.Label(rows.Next(22f),
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Clothing.Note", "Clothing overrides are stored in the policy record; automatic application is not implemented yet."));
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
        }
    }
}
