using System;
using System.Collections.Generic;

namespace OrderedWork.Core
{
    /// <summary>
    /// Pure overlay policy for hourly work focus. The pawn's stored order is always
    /// the base; focus only prepends valid entries and never mutates that order.
    /// </summary>
    public static class HourFocusStack
    {
        public const int MaxFocus = 3;

        public static List<string> NormalizeFocus(IEnumerable<string> focus)
        {
            List<string> result = new List<string>();
            if (focus == null)
            {
                return result;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in focus)
            {
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    continue;
                }
                result.Add(id);
                if (result.Count >= MaxFocus)
                {
                    break;
                }
            }
            return result;
        }

        public static List<string> BuildEffectiveOrder(
            IReadOnlyList<string> baseOrder,
            IReadOnlyList<string> focus,
            Func<string, bool>? isAvailable = null)
        {
            List<string> effective = new List<string>();
            HashSet<string> baseIds = new HashSet<string>(StringComparer.Ordinal);
            if (baseOrder != null)
            {
                foreach (string id in baseOrder)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        baseIds.Add(id);
                    }
                }
            }

            List<string> normalizedFocus = NormalizeFocus(focus);
            HashSet<string> emitted = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in normalizedFocus)
            {
                if (!baseIds.Contains(id) || emitted.Contains(id))
                {
                    continue;
                }
                if (isAvailable != null && !isAvailable(id))
                {
                    continue;
                }
                effective.Add(id);
                emitted.Add(id);
            }

            if (baseOrder != null)
            {
                foreach (string id in baseOrder)
                {
                    if (string.IsNullOrWhiteSpace(id) || (isAvailable != null && !isAvailable(id)) || !emitted.Add(id))
                    {
                        continue;
                    }
                    effective.Add(id);
                }
            }

            return effective;
        }
    }
}
