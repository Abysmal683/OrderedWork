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
        private bool level;
        private bool passion;
        private bool expertise;
        private bool mods;

        public Dialog_SkillFilter(bool level, bool passion, bool expertise, bool mods,
            Action<bool, bool, bool, bool> apply)
        {
            this.level = level;
            this.passion = passion;
            this.expertise = expertise;
            this.mods = mods;
            this.apply = apply;
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(270f, 210f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "Filtro de habilidades");
            Text.Font = GameFont.Small;

            Widgets.CheckboxLabeled(new Rect(8f, 38f, inRect.width - 16f, 24f), "Nivel", ref level);
            Widgets.CheckboxLabeled(new Rect(8f, 66f, inRect.width - 16f, 24f), "Pasión", ref passion);
            Widgets.CheckboxLabeled(new Rect(8f, 94f, inRect.width - 16f, 24f), "Incluir mods", ref mods);

            bool expertiseAvailable = false;
            Widgets.CheckboxLabeled(new Rect(8f, 122f, inRect.width - 16f, 24f),
                "Expertise (VSE)", ref expertise, expertiseAvailable);
            if (!expertiseAvailable)
            {
                TooltipHandler.TipRegion(new Rect(8f, 122f, inRect.width - 16f, 24f),
                    "La integración de Expertise se habilitará cuando el adaptador VSE esté activo.");
            }

            Rect buttons = new Rect(0f, inRect.yMax - 30f, inRect.width, 28f);
            Rect[] cells = new Rect[]
            {
                new Rect(buttons.x, buttons.y, 110f, 28f),
                new Rect(buttons.xMax - 90f, buttons.y, 90f, 28f)
            };
            if (Widgets.ButtonText(cells[0], "Aplicar"))
            {
                apply?.Invoke(level, passion, expertise, mods);
                Close();
            }
            if (Widgets.ButtonText(cells[1], "Cerrar"))
            {
                Close();
            }
        }
    }
}
