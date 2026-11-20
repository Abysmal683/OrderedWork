#nullable disable
using System;
using OrderedWork.Config;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    internal static class ScheduleVisuals
    {
        internal static string ShortLabel(TimeAssignmentDef def)
        {
            return ShortLabel(def, 999f);
        }

        internal static string ShortLabel(TimeAssignmentDef def, float width)
        {
            if (def == null)
            {
                return "—";
            }
            string label = def.LabelCap.ToString();
            if (string.IsNullOrEmpty(label))
            {
                label = def.defName ?? "—";
            }
            if (width < 36f)
            {
                return label.Length <= 1 ? label : label.Substring(0, 1).ToUpperInvariant();
            }
            if (width < 54f && label.Length > 3)
            {
                return label.Substring(0, 3);
            }
            return label.Length <= 5 ? label : label.Substring(0, 5);
        }

        internal static Color AssignmentColor(TimeAssignmentDef def)
        {
            if (def == null)
            {
                return new Color(0.18f, 0.18f, 0.20f, 1f);
            }
            Color color = def.color;
            if (color.a <= 0.01f || (color.r <= 0.01f && color.g <= 0.01f && color.b <= 0.01f))
            {
                int hash = 0;
                string name = def.defName ?? def.label ?? OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Common.Unknown", "unknown");
                for (int i = 0; i < name.Length; i++)
                {
                    hash = (hash * 31 + name[i]) & 0x7fffffff;
                }
                color = Color.HSVToRGB((hash % 360) / 360f, 0.55f, 0.75f);
            }
            color.a = 1f;
            return color;
        }

        internal static void DrawCurrentHourHighlight(float top, float bottom, float hourWidth, int hour)
        {
            if (hour < 0 || hour >= SchedulePreset.SlotCount || bottom <= top)
            {
                return;
            }
            float x = 76f + hour * hourWidth;
            Widgets.DrawBoxSolid(new Rect(x, top, hourWidth - 1f, bottom - top),
                new Color(0.08f, 0.09f, 0.10f, 0.38f));
            Widgets.DrawBoxSolid(new Rect(x, top, 2f, bottom - top),
                new Color(0.38f, 0.40f, 0.43f, 0.45f));
            Widgets.DrawBoxSolid(new Rect(x + hourWidth - 3f, top, 2f, bottom - top),
                new Color(0.38f, 0.40f, 0.43f, 0.45f));
        }
    }
}