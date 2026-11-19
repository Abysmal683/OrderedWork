using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>
    /// Optional VSE/Alpha Skills expertise bridge. No VSE type is referenced
    /// directly; all access is reflection so the mod remains optional.
    /// </summary>
    public static class VseExpertiseAdapter
    {
        public sealed class Row
        {
            public string Label;
            public string Description;
            public string SkillLabel;
            public int Level;
            public float XpCurrent;
            public float XpRequired;
        }

        private static bool resolved;
        private static Type trackersType;
        private static Type uiType;
        private static MethodInfo expertiseOfPawn;
        private static MethodInfo doExpertisePanel;

        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return trackersType != null && uiType != null;
            }
        }

        public static List<Row> GetRows(Pawn pawn)
        {
            Resolve();
            List<Row> result = new List<Row>();
            if (pawn == null || expertiseOfPawn == null)
            {
                return result;
            }
            try
            {
                object tracker = expertiseOfPawn.Invoke(null, new object[] { pawn });
                if (tracker == null)
                {
                    return result;
                }
                PropertyInfo all = tracker.GetType().GetProperty("AllExpertise",
                    BindingFlags.Public | BindingFlags.Instance);
                IEnumerable records = all != null ? all.GetValue(tracker) as IEnumerable : null;
                if (records == null)
                {
                    return result;
                }
                foreach (object record in records)
                {
                    if (record == null)
                    {
                        continue;
                    }
                    FieldInfo defField = record.GetType().GetField("def",
                        BindingFlags.Public | BindingFlags.Instance);
                    object def = defField != null ? defField.GetValue(record) : null;
                    Def expertiseDef = def as Def;
                    if (expertiseDef == null)
                    {
                        continue;
                    }
                    FieldInfo skillField = def.GetType().GetField("skill",
                        BindingFlags.Public | BindingFlags.Instance);
                    SkillDef skill = skillField != null ? skillField.GetValue(def) as SkillDef : null;
                    PropertyInfo levelProperty = record.GetType().GetProperty("Level",
                        BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo xpField = record.GetType().GetField("XpSinceLastLevel",
                        BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo requiredField = record.GetType().GetField("XpRequiredForLevelUp",
                        BindingFlags.Public | BindingFlags.Instance);
                    result.Add(new Row
                    {
                        Label = expertiseDef.LabelCap,
                        Description = expertiseDef.description,
                        SkillLabel = skill != null ? skill.label : "",
                        Level = levelProperty != null ? Convert.ToInt32(levelProperty.GetValue(record)) : 0,
                        XpCurrent = xpField != null ? Convert.ToSingle(xpField.GetValue(record)) : 0f,
                        XpRequired = requiredField != null ? Convert.ToSingle(requiredField.GetValue(record)) : 0f
                    });
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] VSE expertise read failed: " + exception.Message);
            }
            return result;
        }

        public static bool OpenPanel(Pawn pawn)
        {
            Resolve();
            if (pawn == null || doExpertisePanel == null)
            {
                return false;
            }
            Find.WindowStack.Add(new Dialog_VseExpertise(pawn, doExpertisePanel));
            return true;
        }

        private static void Resolve()
        {
            if (resolved)
            {
                return;
            }
            resolved = true;
            trackersType = FindType("VSE.Expertise.ExpertiseTrackers");
            uiType = FindType("VSE.Expertise.ExpertiseUIUtility");
            if (trackersType == null || uiType == null)
            {
                Log.Message("[Ordered Work] VSE expertise adapter unavailable: trackers="
                    + (trackersType != null) + ", ui=" + (uiType != null));
            }
            if (trackersType != null)
            {
                expertiseOfPawn = trackersType.GetMethod("Expertise",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Pawn) }, null);
            }
            if (uiType != null)
            {
                doExpertisePanel = uiType.GetMethod("DoExpertisePanel",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Rect), typeof(Pawn) }, null);
            }
        }

        private static Type FindType(string fullName)
        {
            Type known = AccessTools.TypeByName(fullName);
            if (known != null)
            {
                return known;
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
    }

    /// <summary>Own window wrapper so VSE draws and owns its expertise panel.</summary>
    public sealed class Dialog_VseExpertise : Window
    {
        private readonly Pawn pawn;
        private readonly MethodInfo drawMethod;

        public Dialog_VseExpertise(Pawn pawn, MethodInfo drawMethod)
        {
            this.pawn = pawn;
            this.drawMethod = drawMethod;
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(460f, 520f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                drawMethod.Invoke(null, new object[] { inRect, pawn });
            }
            catch (Exception exception)
            {
                Log.Warning("[Ordered Work] VSE expertise panel failed: " + exception.Message);
            }
        }
    }
}
