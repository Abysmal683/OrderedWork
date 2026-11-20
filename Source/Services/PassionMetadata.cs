using System;
using RimWorld;

namespace OrderedWork.Services
{
    /// <summary>
    /// Optional VSE/Alpha Skills labels, descriptions, colours and gameplay metadata.
    /// No texture or UI drawing belongs here.
    /// </summary>
    public static class PassionMetadata
    {
        public static string DefNameOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def != null)
            {
                string? name = PassionRegistry.GetMember<string>(def, "defName");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            if (passion == Passion.Major) return "Major";
            if (passion == Passion.Minor) return "Minor";
            return "None";
        }

        public static string LabelOf(Passion passion)
        {
            string? label = VseLabelOf(passion);
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }
            return passion == Passion.Major ? "Major" : passion == Passion.Minor ? "Minor" : "None";
        }

        /// <summary>Returns the VSE label only when the optional mod is present.</summary>
        public static string? VseLabelOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return null;
            }
            try
            {
                string? label = PassionRegistry.GetStringMember(def, "LabelCap");
                if (string.IsNullOrEmpty(label))
                {
                    label = PassionRegistry.GetStringMember(def, "label");
                }
                return string.IsNullOrEmpty(label) ? null : label;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string? DescriptionOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return null;
            }
            string? description = PassionRegistry.GetStringMember(def, "description");
            if (string.IsNullOrEmpty(description))
            {
                description = PassionRegistry.GetStringMember(def, "Description");
            }
            return string.IsNullOrEmpty(description) ? null : description;
        }

        public static bool IsBadOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return false;
            }
            object? value = PassionRegistry.GetMember<object>(def, "isBad");
            if (value == null)
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static float LearnRateOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return 0f;
            }
            object? value = PassionRegistry.GetMember<object>(def, "learnRateFactor");
            if (value == null)
            {
                return 0f;
            }
            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        /// <summary>Optional VSE passion colour enum name. Returns null without VSE.</summary>
        public static string? ColorNameOf(Passion passion)
        {
            object? def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return null;
            }
            try
            {
                object? value = PassionRegistry.GetMember<object>(def, "color");
                return value != null ? value.ToString() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
