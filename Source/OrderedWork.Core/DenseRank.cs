using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderedWork.Core
{
    public static class DenseRank
    {
        /// <summary>
        /// Keeps the requested order first, drops unknown/duplicate ids, then appends
        /// every available id that was not requested. The result has no duplicates.
        /// </summary>
        public static List<string> NormalizeOrder(IEnumerable<string> ordered, IEnumerable<string> available)
        {
            List<string> availableList = (available ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            HashSet<string> availableSet = new HashSet<string>(availableList, StringComparer.Ordinal);
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string id in ordered ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(id) || !availableSet.Contains(id) || !seen.Add(id))
                {
                    continue;
                }
                result.Add(id);
            }

            foreach (string id in availableList)
            {
                if (seen.Add(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        /// <summary>
        /// Dense rank: first item = 1, second = 2, ... with no gaps.
        /// </summary>
        public static Dictionary<string, int> BuildRanks(IEnumerable<string> ordered, IEnumerable<string> available)
        {
            List<string> normalized = NormalizeOrder(ordered, available);
            Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < normalized.Count; i++)
            {
                ranks[normalized[i]] = i + 1;
            }
            return ranks;
        }
    }
}
