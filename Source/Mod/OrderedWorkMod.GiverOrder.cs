using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        internal void ResetGiverOrderDraft()
        {
            orderEditorPanel.Reset();
        }
    }
}