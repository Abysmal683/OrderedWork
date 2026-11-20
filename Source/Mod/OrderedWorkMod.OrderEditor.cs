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
        internal void DrawOrderEditor(Listing_Standard list)
        {
            EnsureSplitMergeDefaults();
            list.Label(ConfigText("OrderedWork.Config.Order.Info", "Giver order: choose a WorkType, then drag the destination list. Top = first."));
            Rect selectorRect = list.GetRect(28f);
            DrawWorkTypeSelector(selectorRect, ConfigText("OrderedWork.Config.Order.WorkType", "Work type: "), selectedGiverType, ConfigText("OrderedWork.Config.Order.SelectWorkType", "Select Work type"), delegate(string defName)
            {
                selectedGiverType = defName;
                orderEditorPanel.Reset();
            });

            orderEditorPanel.Draw(list, this, activeProfileName,
                delegate { return selectedGiverType; },
                delegate(string value) { selectedGiverType = value; },
                delegate(string value) { profileStatus = value; });
            DrawInlineWarning(list, string.IsNullOrEmpty(selectedGiverType)
                ? ConfigText("OrderedWork.Config.Order.NoSelectionWarning", "Select a WorkType with out-of-order Givers.")
                : ConfigText("OrderedWork.Config.Order.ReorderWarning", "Apply reorders only the Givers already present."));
            list.Gap(10f);
        }


    }
}