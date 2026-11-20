using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;
using System.Text;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void ApplyLiveProfileFromSettings()
        {
            if (Settings == null || string.IsNullOrEmpty(activeProfileName))
            {
                return;
            }
            WorkConfigProfile profile = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            ResetGiverOrderDraft();
        }






        private void PersistLivePending()
        {
            if (Settings == null || string.IsNullOrEmpty(activeProfileName))
            {
                return;
            }
            WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
            WorkConfigStore.SavePending(captured);
        }


        private static void ReturnSurplusGivers(
            WorkConfigProfile display,
            WorkConfigProfile reference,
            string targetDefName,
            HashSet<string> targetGivers,
            WorkTypeConfig currentTarget)
        {
            if (display == null || reference == null || currentTarget == null || currentTarget.Givers == null)
            {
                return;
            }
            foreach (GiverConfig giver in currentTarget.Givers.ToList())
            {
                if (giver == null || targetGivers.Contains(giver.DefName))
                {
                    continue;
                }
                string ownerName = null;
                foreach (KeyValuePair<string, WorkTypeConfig> entry in reference.WorkTypes)
                {
                    if (entry.Value != null && entry.Value.Givers != null
                        && entry.Value.Givers.Any(g => g != null && g.DefName == giver.DefName))
                    {
                        ownerName = entry.Key;
                        break;
                    }
                }
                WorkTypeConfig ownerConfig;
                if (string.IsNullOrEmpty(ownerName) || ownerName == targetDefName
                    || !display.WorkTypes.TryGetValue(ownerName, out ownerConfig)
                    || ownerConfig == null)
                {
                    OrderedWorkMod.DebugLog("[Reset] surplus giver dropped " + giver.DefName);
                    continue;
                }
                ownerConfig.Givers = ownerConfig.Givers ?? new List<GiverConfig>();
                if (!ownerConfig.Givers.Any(g => g != null && g.DefName == giver.DefName))
                {
                    ownerConfig.Givers.Add(new GiverConfig
                    {
                        DefName = giver.DefName,
                        Order = ownerConfig.Givers.Count,
                        Mod = giver.Mod
                    });
                }
                WorkGiverDef live = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giver.DefName);
                WorkTypeDef ownerDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(ownerName);
                if (live != null && ownerDef != null)
                {
                    live.workType = ownerDef;
                }
            }
        }

        private static void RemoveGiverFromOtherTypes(WorkConfigProfile profile, string giverDefName, string exceptDefName)
        {
            if (profile == null || profile.WorkTypes == null || string.IsNullOrEmpty(giverDefName))
            {
                return;
            }
            List<string> emptySplits = new List<string>();
            foreach (KeyValuePair<string, WorkTypeConfig> entry in profile.WorkTypes)
            {
                if (entry.Key == exceptDefName || entry.Value == null || entry.Value.Givers == null)
                {
                    continue;
                }
                entry.Value.Givers.RemoveAll(g => g != null && g.DefName == giverDefName);
                for (int i = 0; i < entry.Value.Givers.Count; i++)
                {
                    entry.Value.Givers[i].Order = i;
                }
                if (!entry.Value.Vanilla
                    && !string.IsNullOrEmpty(entry.Value.Base)
                    && entry.Value.Givers.Count == 0)
                {
                    emptySplits.Add(entry.Key);
                }
            }
            foreach (string emptySplit in emptySplits)
            {
                profile.WorkTypes.Remove(emptySplit);
                OrderedWorkMod.DebugLog("[Reset] removed empty split " + emptySplit);
            }
        }

        private static WorkTypeConfig CloneWorkType(WorkTypeConfig source)
        {
            if (source == null)
            {
                return null;
            }
            return new WorkTypeConfig
            {
                Vanilla = source.Vanilla,
                Base = source.Base,
                Label = source.Label,
                ActionWord = source.ActionWord,
                Description = source.Description,
                Visible = source.Visible,
                Order = source.Order,
                Mod = source.Mod,
                Givers = source.Givers == null
                    ? new List<GiverConfig>()
                    : source.Givers.Select(g => new GiverConfig
                    {
                        DefName = g.DefName,
                        Order = g.Order,
                        Mod = g.Mod
                    }).ToList()
            };
        }


    }
}