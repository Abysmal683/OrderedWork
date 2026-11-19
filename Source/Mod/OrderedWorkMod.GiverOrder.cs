using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Services;
using OrderedWork.UI;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    public partial class OrderedWorkMod : Mod
    {
        private void ResetGiverOrderDraft()
        {
            giverOrderDraftInitialized = false;
            giverOrderDirty = false;
            giverOrderDraft.Clear();
            giverOrderInitial.Clear();
            giverOrderDraftType = "";
        }

        private void MoveGiverOrderDraft(int from, int to)
        {
            if (from < 0 || from >= giverOrderDraft.Count || to < 0 || to >= giverOrderDraft.Count || from == to)
            {
                return;
            }
            string item = giverOrderDraft[from];
            giverOrderDraft.RemoveAt(from);
            giverOrderDraft.Insert(to, item);
            giverOrderDirty = true;
        }

        private void ApplyGiverOrderDraft(WorkConfigProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(selectedGiverType)
                || !profile.WorkTypes.TryGetValue(selectedGiverType, out WorkTypeConfig current)
                || current == null)
            {
                return;
            }
            current.Givers = current.Givers ?? new List<GiverConfig>();
            for (int i = 0; i < giverOrderDraft.Count; i++)
            {
                GiverConfig giver = current.Givers.FirstOrDefault(g => g != null && g.DefName == giverOrderDraft[i]);
                if (giver != null)
                {
                    giver.Order = i;
                }
            }
            current.Givers = current.Givers.OrderBy(g => g.Order).ToList();
            WorkConfigStore.SaveProfile(profile, true);
            WorkConfigStore.SavePending(profile);
            OrderedWorkStartup.ApplyProfileNow(profile, Settings, this);
            profileStatus = string.Format(ConfigText("OrderedWork.Config.GiverOrder.Applied", "Giver order for {0} applied."), selectedGiverType);
            Messages.Message(string.Format(ConfigText("OrderedWork.Config.GiverOrder.Saved", "Giver order saved for {0}."), selectedGiverType), MessageTypeDefOf.TaskCompletion, false);
            giverOrderDraftInitialized = false;
            giverOrderDirty = false;
        }
    }
}