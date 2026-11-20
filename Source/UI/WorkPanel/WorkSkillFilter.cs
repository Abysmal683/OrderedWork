#nullable disable
namespace OrderedWork.UI
{
    /// <summary>Persistent skill-list ordering flags shared by the panel and filter dialog.</summary>
    public sealed class WorkSkillFilter
    {
        public bool Level = true;
        public bool Passion = true;
        public bool Expertise;
        public bool Mods = true;

        public void Load(OrderedWorkSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            Level = settings.skillFilterLevel;
            Passion = settings.skillFilterPassion;
            Expertise = settings.skillFilterExpertise;
            Mods = settings.skillFilterMods;
        }

        public void Apply(bool level, bool passion, bool expertise, bool mods, OrderedWorkSettings settings)
        {
            Level = level;
            Passion = passion;
            Expertise = expertise;
            Mods = mods;
            if (settings == null)
            {
                return;
            }
            settings.skillFilterLevel = level;
            settings.skillFilterPassion = passion;
            settings.skillFilterExpertise = expertise;
            settings.skillFilterMods = mods;
        }
    }
}