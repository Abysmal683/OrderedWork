using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>Small checkbox menu for the skill ordering filters.</summary>
    public sealed class Dialog_SkillFilter : Window
    {
        private readonly Action<bool, bool, bool, bool> apply;
        private readonly bool expertiseAvailable;
        private bool level;
        private bool passion;
        private bool expertise;
        private bool mods;

        public Dialog_SkillFilter(bool level, bool passion, bool expertise, bool mods,
            bool expertiseAvailable, Action<bool, bool, bool, bool> apply)
        {
            this.level = level;
            this.passion = passion;
            this.expertise = expertise;
            this.mods = mods;
            this.expertiseAvailable = expertiseAvailable;
            this.apply = apply;
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnCancel = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(270f, 210f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Skill filters");
            Text.Font = GameFont.Small;

            bool oldLevel = level;
            bool oldPassion = passion;
            bool oldMods = mods;
            bool oldExpertise = expertise;
            Widgets.CheckboxLabeled(new Rect(8f, 38f, inRect.width - 16f, 24f), "Level", ref level);
            Widgets.CheckboxLabeled(new Rect(8f, 66f, inRect.width - 16f, 24f), "Passion", ref passion);
            Widgets.CheckboxLabeled(new Rect(8f, 94f, inRect.width - 16f, 24f), "Include mods", ref mods);
            Widgets.CheckboxLabeled(new Rect(8f, 122f, inRect.width - 16f, 24f),
                "Expertise (VSE)", ref expertise, !expertiseAvailable);
            if (!expertiseAvailable)
            {
                TooltipHandler.TipRegion(new Rect(8f, 122f, inRect.width - 16f, 24f),
                    "Expertise will be enabled when the VSE adapter is active.");
            }

            if (level != oldLevel || passion != oldPassion || mods != oldMods || expertise != oldExpertise)
            {
                apply?.Invoke(level, passion, expertise, mods);
            }


        }
    }
}
