#nullable disable
using OrderedWork.Config;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void PersistLivePending()
        {
            if (Settings == null || string.IsNullOrEmpty(activeProfileName))
            {
                return;
            }
            WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
            WorkConfigStore.SavePending(captured);
        }
    }
}