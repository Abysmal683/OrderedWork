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
            labelKey = "Perfil";
            size = new Vector2(400f, 220f);
            tutorTag = "OrderedWorkProfile";
        }

        public override bool IsVisible
        {
            get { return SelPawn != null && SelPawn.IsFreeColonist && !SelPawn.Dead; }
        }

        public override void OnOpen()
        {
            OpenDetail();
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y);
            Widgets.Label(rect.ContractedBy(12f), "Perfil de Ordered Work");
            if (Widgets.ButtonText(new Rect(12f, 44f, 180f, 28f), "Abrir detalle"))
            {
                OpenDetail();
            }
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
