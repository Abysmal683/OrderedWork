#nullable disable
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace OrderedWork.Services
{
    /// <summary>Per-pawn stat tooltip cache using RimWorld's live explanation.</summary>
    public sealed class StatTooltipService
    {
        private readonly Pawn pawn;
        private readonly Dictionary<string, string> cache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int cacheTick = -1;

        public StatTooltipService(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void Tick()
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticks - cacheTick >= 60)
            {
                cache.Clear();
                cacheTick = ticks;
            }
        }

        public string Get(StatDef stat)
        {
            if (stat == null)
            {
                return "Stat";
            }
            string cached;
            if (cache.TryGetValue(stat.defName, out cached))
            {
                return cached;
            }
            float value = 0f;
            try
            {
                value = pawn != null ? pawn.GetStatValue(stat) : 0f;
            }
            catch (Exception)
            {
            }
            string explanation = null;
            try
            {
                if (stat.Worker != null)
                {
                    explanation = stat.Worker.GetExplanationFull(
                        StatRequest.For(pawn), stat.toStringNumberSense, value);
                }
            }
            catch (Exception)
            {
            }
            string result = stat.LabelCap + "\nCurrent: "
                + stat.Worker.ValueToString(value, true, stat.toStringNumberSense);
            if (!string.IsNullOrEmpty(explanation))
            {
                result += "\n" + explanation;
            }
            cache[stat.defName] = result;
            return result;
        }
    }
}