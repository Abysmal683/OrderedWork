using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OrderedWork.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Three-row WorkType palette, drag ghost and drop application. The main tab owns the
    /// drag state; this helper draws the chips/ghost and applies a drop to the target pawns.
    /// VSE/Alpha Skills integration is read-only reflection over their public PassionDef
    /// contract; no third-party source or assets are copied or redistributed. See CREDITS.md.
    /// </summary>
    internal static class WorkTypePaletteView
    {
        internal const float ChipWidth = 110f;
        internal const float ChipHeight = 22f;
        internal const float ChipGap = 3f;
        internal const int Rows = 3;

        // Shared by lane cards, the drag card and Details; change once here.
        internal const float PassionIconSize = 14f;
        private const float HeldCardWidth = 180f;

        // Shared by the vanilla fallback and the VSE/Alpha gray-icon fallback.
        private static readonly Color MinorPassionTint = new Color(1f, 0.86f, 0.30f, 1f);
        private static readonly Color MajorPassionTint = new Color(1f, 0.55f, 0.12f, 1f);
        private static readonly Color DisabledPassionTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        /// <summary>Texture + tint for a passion icon; Texture is null when no icon should be drawn.</summary>
        internal struct PassionVisual
        {
            public Texture2D Texture;
            public Color Tint;
        }

        /// <summary>
        /// Visual-only palette entries for the debug scroll stress test. They are never
        /// registered in DefDatabase and must not be draggable or assignable.
        /// </summary>
        internal static bool IsDebugPaletteType(WorkTypeDef type)
        {
            return type != null
                && !string.IsNullOrEmpty(type.defName)
                && type.defName.StartsWith("OWDebugPalette_", StringComparison.Ordinal);
        }

        private static List<WorkTypeDef> debugPaletteTypes;
        private static int debugPaletteTypeCount;

        internal static List<WorkTypeDef> BuildDebugPaletteTypes(int count)
        {
            // Called every frame while the stress setting is on; build the visual-only
            // defs once so the debug toggle cannot allocate 60 objects per repaint.
            if (debugPaletteTypes != null && debugPaletteTypeCount == count)
            {
                return debugPaletteTypes;
            }

            debugPaletteTypes = new List<WorkTypeDef>();
            debugPaletteTypeCount = count;
            for (int i = 1; i <= count; i++)
            {
                debugPaletteTypes.Add(new WorkTypeDef
                {
                    defName = "OWDebugPalette_" + i,
                    label = "Debug Work " + i,
                    labelShort = "Debug " + i,
                    description = "Visual-only palette stress entry; not assignable.",
                    naturalPriority = 0,
                    visible = true
                });
            }
            return debugPaletteTypes;
        }

        internal static string DisplayName(WorkTypeDef type)
        {
            if (type == null)
            {
                return "";
            }
            string name = !string.IsNullOrEmpty(type.labelShort) ? type.labelShort : type.label;
            if (string.IsNullOrEmpty(name))
            {
                return type.defName;
            }
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        internal static void DrawPalette(
            Rect rect,
            ref Vector2 scroll,
            List<WorkTypeDef> types,
            WorkTypeDef draggingType,
            bool showScrollbar,
            bool suppressTooltips,
            Action<WorkTypeDef> beginDrag)
        {
            if (types == null || types.Count == 0)
            {
                return;
            }

            Rect viewport = new Rect(rect.x, rect.y, rect.width, rect.height - WorkGridScrollView.Thickness);
            int perColumn = Mathf.Max(1, Mathf.CeilToInt(types.Count / (float)Rows));
            float contentWidth = Mathf.Max(viewport.width, perColumn * (ChipWidth + ChipGap) + ChipGap);
            Rect content = new Rect(0f, 0f, contentWidth, viewport.height);
            Rect hTrack = new Rect(rect.x, rect.yMax - WorkGridScrollView.Thickness, rect.width, WorkGridScrollView.Thickness);

            // Shift+wheel gives the palette its own horizontal quick scroll.
            WorkGridScrollView.HandleWheel(viewport, viewport, content, ref scroll);

            // Handle the bar before the scroll view so the content cannot eat its MouseDown.
            if (showScrollbar)
            {
                WorkGridScrollView.HandleHorizontal(hTrack, viewport, content, ref scroll, typeof(WorkTypePaletteView));
            }

            Widgets.BeginScrollView(viewport, ref scroll, content, false);
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    WorkTypeDef type = types[i];
                    if (type == null)
                    {
                        continue;
                    }

                    // Top-to-bottom inside a column, then left-to-right.
                    int row = i % Rows;
                    int column = i / Rows;
                    Rect chip = new Rect(
                        ChipGap + column * (ChipWidth + ChipGap),
                        row * (ChipHeight + ChipGap),
                        ChipWidth,
                        ChipHeight);

                    Color typeColor = WorkTypeColorRegistry.GetColor(type);
                    Widgets.DrawBoxSolid(
                        chip,
                        type == draggingType
                            ? new Color(typeColor.r, typeColor.g, typeColor.b, 0.55f)
                            : new Color(typeColor.r, typeColor.g, typeColor.b, 0.30f));
                    Widgets.DrawBoxSolid(new Rect(chip.x, chip.y, 2f, chip.height), new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f));

                    if (Mouse.IsOver(chip))
                    {
                        Widgets.DrawHighlight(chip);
                    }

                    bool previousWrap = Text.WordWrap;
                    Text.WordWrap = false;
                    Widgets.LabelEllipses(new Rect(chip.x + 5f, chip.y, chip.width - 9f, chip.height), DisplayName(type));
                    Text.WordWrap = previousWrap;

                    if (!suppressTooltips && draggingType == null && Mouse.IsOver(chip))
                    {
                        TooltipHandler.TipRegion(chip, PaletteTooltip(type));
                    }

                    if (!IsDebugPaletteType(type)
                        && Event.current.type == EventType.MouseDown
                        && Event.current.button == 0
                        && chip.Contains(Event.current.mousePosition))
                    {
                        beginDrag(type);
                        Event.current.Use();
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            if (showScrollbar)
            {
                WorkGridScrollView.DrawHorizontal(hTrack, viewport, content, ref scroll);
            }
        }

        internal static void DrawDragCard(Vector2 mouse, WorkTypeDef type, Pawn hoveredPawn, bool incapable)
        {
            if (type == null)
            {
                return;
            }

            Rect rect = new Rect(mouse.x + 12f, mouse.y + 10f, HeldCardWidth, 28f);
            Color typeColor = WorkTypeColorRegistry.GetColor(type);
            Widgets.DrawBoxSolid(
                rect,
                incapable
                    ? new Color(0.36f, 0.12f, 0.12f, 0.95f)
                    : new Color(typeColor.r * 0.55f, typeColor.g * 0.55f, typeColor.b * 0.55f, 0.95f));

            float x = rect.x + 4f;
            if (hoveredPawn != null && !incapable)
            {
                Passion passion;
                SkillRecord bestSkill = SafeBestSkill(hoveredPawn, type, out passion);
                int level = bestSkill != null ? bestSkill.Level : -1;
                if (level >= 0)
                {
                    PassionVisual visual = ResolvePassionVisual(passion);
                    if (visual.Texture != null)
                    {
                        DrawPassionIcon(new Rect(x, rect.y + 8f, PassionIconSize, PassionIconSize), visual);
                        x += PassionIconSize + 2f;
                    }

                    GameFont previousFont = Text.Font;
                    Text.Font = GameFont.Tiny;
                    Widgets.Label(new Rect(x, rect.y, 22f, rect.height), level.ToString());
                    Text.Font = previousFont;
                    x += 22f;
                }
            }

            string label = incapable ? DisplayName(type) + " (Incapaz)" : DisplayName(type);
            bool previousWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.LabelEllipses(new Rect(x, rect.y, Mathf.Max(10f, rect.xMax - x - 4f), rect.height), label);
            Text.WordWrap = previousWrap;
        }

        internal static void DrawGhostSlot(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.28f, 0.34f, 0.55f));
            Color previous = GUI.color;
            GUI.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            Widgets.DrawBox(rect, 1);
            GUI.color = previous;
        }

        internal static void ApplyDrop(WorkTypeDef type, Pawn hoveredPawn, int dropIndex, List<Pawn> selected)
        {
            if (type == null)
            {
                return;
            }

            List<Pawn> targets = selected != null && selected.Count > 0
                ? selected
                : (hoveredPawn != null ? new List<Pawn> { hoveredPawn } : new List<Pawn>());
            if (targets.Count == 0)
            {
                return;
            }

            int skipped = 0;
            foreach (Pawn pawn in targets)
            {
                if (pawn == null || pawn.Destroyed || !pawn.IsFreeColonist)
                {
                    skipped++;
                    continue;
                }
                if (WorkOrderService.IsIncapable(pawn, type))
                {
                    skipped++;
                    continue;
                }

                List<string> ids = WorkOrderService.GetOrder(pawn);
                int index = ids.IndexOf(type.defName);
                int target = Mathf.Clamp(dropIndex, 0, ids.Count);

                if (index >= 0)
                {
                    if (target == index || target == index + 1)
                    {
                        continue;
                    }
                    ids.RemoveAt(index);
                    if (target > index)
                    {
                        target--;
                    }
                    ids.Insert(Mathf.Clamp(target, 0, ids.Count), type.defName);
                }
                else
                {
                    ids.Insert(target, type.defName);
                }

                WorkOrderService.SaveAndApply(pawn, ids);
            }

            if (skipped > 0)
            {
                Messages.Message(
                    "Ordered Work: " + skipped + " selected pawn(s) cannot do " + DisplayName(type) + " (Incapaz).",
                    MessageTypeDefOf.RejectInput,
                    false);
            }
        }

        private static string PaletteTooltip(WorkTypeDef type)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(DisplayName(type));
            builder.Append(" (").Append(type.defName).Append(")");
            if (!string.IsNullOrEmpty(type.description))
            {
                builder.AppendLine().AppendLine(type.description);
            }

            List<WorkGiverDef> givers = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(g => g.workType == type)
                .OrderByDescending(g => g.priorityInType)
                .ToList();
            builder.AppendLine();
            builder.AppendLine("Work givers:");
            if (givers.Count == 0)
            {
                builder.AppendLine("- none");
            }
            else
            {
                foreach (WorkGiverDef giver in givers)
                {
                    builder.AppendLine("- " + giver.defName);
                }
            }

            builder.AppendLine();
            builder.Append("Relevant skills: ");
            if (type.relevantSkills == null || type.relevantSkills.Count == 0)
            {
                builder.Append("none");
            }
            else
            {
                bool first = true;
                for (int i = 0; i < type.relevantSkills.Count; i++)
                {
                    SkillDef skill = type.relevantSkills[i];
                    if (skill == null)
                    {
                        continue;
                    }
                    if (!first)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(skill.label);
                    first = false;
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Best relevant SkillRecord without calling Pawn_SkillTracker.GetSkill for a missing
        /// skill (that call logs a fallback warning). Returns null when no record is present.
        /// </summary>
        internal static SkillRecord SafeBestSkill(Pawn pawn, WorkTypeDef workType, out Passion passion)
        {
            passion = Passion.None;
            if (pawn == null || pawn.Dead || pawn.skills == null || pawn.skills.skills == null
                || workType == null || workType.relevantSkills == null)
            {
                return null;
            }

            SkillRecord best = null;
            foreach (SkillDef skill in workType.relevantSkills)
            {
                if (skill == null)
                {
                    continue;
                }

                SkillRecord record = null;
                List<SkillRecord> records = pawn.skills.skills;
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null && records[i].def == skill)
                    {
                        record = records[i];
                        break;
                    }
                }
                if (record == null)
                {
                    continue; // do not call GetSkill, which logs a fallback
                }

                if (best == null
                    || record.Level > best.Level
                    || (record.Level == best.Level && PassionRank(record.passion) > PassionRank(best.passion)))
                {
                    best = record;
                }
            }

            if (best != null)
            {
                passion = best.passion;
            }
            return best;
        }

        private static int PassionRank(Passion passion)
        {
            if (passion == Passion.None)
            {
                return 0;
            }
            int colorRank = ColorRankOf(passion);
            if (colorRank > 0)
            {
                return colorRank;
            }
            if (passion == Passion.Major)
            {
                return 4;
            }
            if (passion == Passion.Minor)
            {
                return 3;
            }
            return 0;
        }

        internal static string PassionText(Passion passion)
        {
            string vseLabel = TryGetVseLabel(passion);
            if (!string.IsNullOrEmpty(vseLabel))
            {
                return vseLabel;
            }
            if (passion == Passion.Major)
            {
                return "major";
            }
            if (passion == Passion.Minor)
            {
                return "minor";
            }
            if (passion == Passion.None)
            {
                return "none";
            }
            return passion.ToString();
        }

        /// <summary>Distinct warm tint for the vanilla monochrome passion icons.</summary>
        internal static Color PassionColor(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return MajorPassionTint;
            }
            if (passion == Passion.Minor)
            {
                return MinorPassionTint;
            }
            return Color.white;
        }

        /// <summary>
        /// Resolves the icon texture/tint for a passion. Uses VSE/Alpha Skills when loaded:
        /// colored <c>PassionDef.Icon</c> first, then the gray <c>WorkBoxIcon</c> tinted by
        /// <c>PassionDef.color</c>. Without VSE it uses the vanilla colored textures, falling
        /// back to the vanilla gray work-box icons with the existing warm tints. Never draws
        /// an icon for <see cref="Passion.None"/>; never throws or spams the log.
        /// </summary>
        internal static PassionVisual ResolvePassionVisual(Passion passion)
        {
            if (passion == Passion.None)
            {
                return default(PassionVisual);
            }

            int key = ToInt(passion);
            PassionVisual cached;
            if (passionVisualCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            PassionVisual visual = ComputePassionVisual(passion);
            passionVisualCache[key] = visual;
            return visual;
        }

        internal static void DrawPassionIcon(Rect rect, Passion passion)
        {
            DrawPassionIcon(rect, ResolvePassionVisual(passion));
        }

        internal static void DrawPassionIcon(Rect rect, PassionVisual visual)
        {
            if (visual.Texture == null)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = visual.Tint;
            GUI.DrawTexture(rect, visual.Texture);
            GUI.color = previous;
        }

        private static PassionVisual ComputePassionVisual(Passion passion)
        {
            object passionDef = GetVsePassionDef(passion);
            if (passionDef != null)
            {
                Texture2D colored = GetDefTexture(iconProperty, passionDef);
                if (colored != null)
                {
                    PassionVisual result = new PassionVisual();
                    result.Texture = colored;
                    result.Tint = Color.white;
                    return result;
                }

                Texture2D workBox = GetDefTexture(workBoxIconProperty, passionDef);
                if (workBox != null)
                {
                    PassionVisual result = new PassionVisual();
                    result.Texture = workBox;
                    result.Tint = TintForPassionColor(passionDef);
                    return result;
                }
            }

            // Vanilla colored textures first, then the vanilla gray work-box icons.
            Texture2D vanillaColored = VanillaColoredTexture(passion);
            if (vanillaColored != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaColored;
                result.Tint = Color.white;
                return result;
            }

            Texture2D vanillaGray = VanillaGrayTexture(passion);
            if (vanillaGray != null)
            {
                PassionVisual result = new PassionVisual();
                result.Texture = vanillaGray;
                result.Tint = PassionColor(passion);
                return result;
            }

            return default(PassionVisual);
        }

        private static Texture2D VanillaColoredTexture(Passion passion)
        {
            ResolveVanillaTextures();
            if (passion == Passion.Major)
            {
                return vanillaMajorColored;
            }
            if (passion == Passion.Minor)
            {
                return vanillaMinorColored;
            }
            return null;
        }

        private static Texture2D VanillaGrayTexture(Passion passion)
        {
            if (passion == Passion.Major)
            {
                return WidgetsWork.PassionWorkboxMajorIcon;
            }
            if (passion == Passion.Minor)
            {
                return WidgetsWork.PassionWorkboxMinorIcon;
            }
            return null;
        }

        private static void ResolveVanillaTextures()
        {
            if (vanillaTexturesResolved)
            {
                return;
            }
            vanillaTexturesResolved = true;
            try
            {
                vanillaMinorColored = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor", false);
                vanillaMajorColored = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor", false);
            }
            catch (Exception)
            {
                vanillaMinorColored = null;
                vanillaMajorColored = null;
            }
        }

        private static Texture2D GetDefTexture(PropertyInfo property, object passionDef)
        {
            if (property == null || passionDef == null)
            {
                return null;
            }
            try
            {
                return property.GetValue(passionDef, null) as Texture2D;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Color TintForPassionColor(object passionDef)
        {
            string colorName = PassionColorName(passionDef);
            switch (colorName)
            {
                case "Minor":
                    return MinorPassionTint;
                case "Major":
                    return MajorPassionTint;
                case "Disabled":
                    return DisabledPassionTint;
                case "Main":
                default:
                    return Color.white;
            }
        }

        private static string PassionColorName(object passionDef)
        {
            if (passionDef == null || colorField == null)
            {
                return "";
            }
            try
            {
                object value = colorField.GetValue(passionDef);
                return value != null ? value.ToString() : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static int ColorRankOf(Passion passion)
        {
            int key = ToInt(passion);
            int cached;
            if (colorRankCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            int rank = 0;
            object passionDef = GetVsePassionDef(passion);
            if (passionDef != null)
            {
                switch (PassionColorName(passionDef))
                {
                    case "Major":
                        rank = 4;
                        break;
                    case "Minor":
                        rank = 3;
                        break;
                    case "Main":
                        rank = 2;
                        break;
                    case "Disabled":
                        rank = 1;
                        break;
                }
            }

            colorRankCache[key] = rank;
            return rank;
        }

        private static string TryGetVseLabel(Passion passion)
        {
            object passionDef = GetVsePassionDef(passion);
            if (passionDef == null)
            {
                return null;
            }

            // PassionDef extends Verse.Def; the inherited LabelCap is the reliable source.
            Def def = passionDef as Def;
            if (def != null && !string.IsNullOrEmpty(def.LabelCap))
            {
                return def.LabelCap;
            }

            try
            {
                PropertyInfo property = labelCapProperty ?? passionDef.GetType().GetProperty("LabelCap");
                if (property != null)
                {
                    string labelCap = property.GetValue(passionDef, null) as string;
                    if (!string.IsNullOrEmpty(labelCap))
                    {
                        return labelCap;
                    }
                }

                FieldInfo labelField = passionDef.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                string label = labelField != null ? labelField.GetValue(passionDef) as string : null;
                return string.IsNullOrEmpty(label) ? null : label.CapitalizeFirst();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object GetVsePassionDef(Passion passion)
        {
            if (passion == Passion.None)
            {
                return null;
            }
            ResolveVseApi();
            if (!vseAvailable || passionToDefMethod == null)
            {
                return null;
            }
            try
            {
                return passionToDefMethod.Invoke(null, new object[] { passion });
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ResolveVseApi()
        {
            if (vseResolved)
            {
                return;
            }
            vseResolved = true;

            try
            {
                Type managerType = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    try
                    {
                        managerType = assemblies[i].GetType("VSE.Passions.PassionManager", false);
                    }
                    catch (Exception)
                    {
                        managerType = null;
                    }
                    if (managerType != null)
                    {
                        break;
                    }
                }

                if (managerType == null)
                {
                    return;
                }

                MethodInfo[] methods = managerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name == "PassionToDef" && methods[i].GetParameters().Length == 1)
                    {
                        passionToDefMethod = methods[i];
                        break;
                    }
                }

                Type passionDefType = managerType.Assembly.GetType("VSE.Passions.PassionDef", false);
                if (passionDefType != null)
                {
                    iconProperty = passionDefType.GetProperty("Icon", BindingFlags.Public | BindingFlags.Instance);
                    workBoxIconProperty = passionDefType.GetProperty("WorkBoxIcon", BindingFlags.Public | BindingFlags.Instance);
                    colorField = passionDefType.GetField("color", BindingFlags.Public | BindingFlags.Instance);
                    labelCapProperty = passionDefType.GetProperty("LabelCap", BindingFlags.Public | BindingFlags.Instance);
                }

                vseAvailable = passionToDefMethod != null;
            }
            catch (Exception)
            {
                vseAvailable = false;
            }
        }

        private static int ToInt(Passion passion)
        {
            try
            {
                return Convert.ToInt32(passion);
            }
            catch (Exception)
            {
                return (int)passion;
            }
        }

        private static bool vseResolved;
        private static bool vseAvailable;
        private static MethodInfo passionToDefMethod;
        private static PropertyInfo iconProperty;
        private static PropertyInfo workBoxIconProperty;
        private static PropertyInfo labelCapProperty;
        private static FieldInfo colorField;

        private static bool vanillaTexturesResolved;
        private static Texture2D vanillaMinorColored;
        private static Texture2D vanillaMajorColored;

        private static readonly Dictionary<int, PassionVisual> passionVisualCache = new Dictionary<int, PassionVisual>();
        private static readonly Dictionary<int, int> colorRankCache = new Dictionary<int, int>();
    }
}
