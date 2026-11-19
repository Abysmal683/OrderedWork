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
        private Vector2 passionWeightScroll;
        private readonly List<string> workOrderDraft = new List<string>();
        private readonly List<string> workOrderBaseline = new List<string>();
        private bool workOrderDraftInitialized;
        private readonly HashSet<string> collapsedSections = new HashSet<string>();

        private bool DrawCollapsibleSection(Listing_Standard list, string key, string title)
        {
            bool expanded = !collapsedSections.Contains(key);
            bool next = UiCollapsibleSection.Header(list, title, expanded);
            if (next != expanded)
            {
                if (next) collapsedSections.Remove(key);
                else collapsedSections.Add(key);
            }
            return next;
        }

        private void EnsureProfileNameInitialized()
        {
            if (profileNameInitialized)
            {
                return;
            }
            activeProfileName = WorkConfigStore.LoadSelectedProfileName();
            newProfileName = activeProfileName;
            profileNameInitialized = true;
        }

        private void DrawSettingsTabs(Rect rect)
        {
            OrderedTabStrip.Draw(rect, new[] { ConfigText("OrderedWork.Config.RulesUI.RulesTab", "Rules"), ConfigText("OrderedWork.Config.RulesUI.GeneralTab", "General") }, settingsTab,
                delegate(int index)
                {
                    if (settingsTab != index)
                    {
                        settingsTab = index;
                        ResetEditorUiState();
                    }
                });
        }

        private static void DrawTabMarker(Rect rect)
        {
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 3f, rect.width, 3f),
                new Color(0.45f, 0.72f, 0.95f, 0.95f));
        }

    }
}