#nullable disable
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Services;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Direct access tab for the per-pawn Ordered Work detail. The tab itself is
    /// intentionally just a launcher; the full editor lives in the existing dialog.
    /// </summary>
    public sealed class ITab_Pawn_Profile : ITab
    {
        public ITab_Pawn_Profile()
        {
            labelKey = "Profile";
            size = new Vector2(400f, 340f);
            tutorTag = "OrderedWorkProfile";
        }

        public override bool IsVisible
        {
            get { return SelPawn != null && SelPawn.IsFreeColonist && !SelPawn.Dead; }
        }



        protected override void FillTab()
        {
            ProfileSummaryPanel.Draw(SelPawn, new Rect(0f, 0f, size.x, size.y), OpenDetail);
        }
        private void OpenDetail()
        {
            if (SelPawn == null || SelPawn.Dead)
            {
                return;
            }
            if (Find.WindowStack.IsOpen<Dialog_WorkControlDetails>())
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_WorkControlDetails(SelPawn));
        }
    }
}