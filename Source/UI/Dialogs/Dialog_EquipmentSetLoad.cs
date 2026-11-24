using System;
using System.Collections.Generic;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.Core;
using OrderedWork.Localization;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Load/store manager for equipment sets. New and delete live here, not in the editor header.
    /// </summary>
    public sealed class Dialog_EquipmentSetLoad : Window
    {
        private readonly Action<EquipmentSet> onLoad;
        private List<EquipmentSet> sets;
        private EquipmentSet? pendingDelete;
        private Vector2 scroll;

        public Dialog_EquipmentSetLoad(Action<EquipmentSet> onLoad)
        {
            this.onLoad = onLoad;
            sets = EquipmentSetStore.LoadAll();
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                int count = sets?.Count ?? 0;
                return new Vector2(640f, Mathf.Clamp(170f + count * 38f, 280f, 600f));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            UiRows rows = new UiRows(UiInset.All(inRect, 10f), 28f, 8f);
            Rect titleRect = rows.Next();
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, OrderedWorkText.T("OrderedWork.Equipment.Editor.LoadTitle", "Equipment sets"));
            Text.Font = GameFont.Small;

            if (UiButtonBar.Button(rows.Next(26f), OrderedWorkText.T("OrderedWork.Equipment.Editor.New", "New")))
            {
                OpenNameDialog();
            }

            if (pendingDelete != null)
            {
                DrawDeleteConfirmation(rows.Remaining);
                return;
            }

            Rect listRect = rows.Remaining;
            List<EquipmentSet> currentSets = sets ?? new List<EquipmentSet>();
            UiScrollPane.Draw(listRect, ref scroll, Mathf.Max(listRect.height, currentSets.Count * 38f + 4f), this, delegate(Rect view)
            {
                UiRows listRows = new UiRows(view, 34f, 4f);
                for (int i = 0; i < currentSets.Count; i++)
                {
                    EquipmentSet item = currentSets[i];
                    Rect row = listRows.Next();
                    UiTable columns = new UiTable(row, 6f);
                    columns.Flex().Add(78f).Add(88f);
                    if (UiCard.Draw(columns.Column(0), item.Name, null, false, null,
                        OrderedWorkText.T("OrderedWork.Equipment.Editor.SelectSetHint", "Select this set.")))
                    {
                        Load(item);
                        return;
                    }
                    if (UiButtonBar.Button(columns.Column(1), OrderedWorkText.T("OrderedWork.Common.Load", "Load")))
                    {
                        Load(item);
                        return;
                    }
                    if (UiButtonBar.Button(columns.Column(2), OrderedWorkText.T("OrderedWork.Common.Delete", "Delete")))
                    {
                        pendingDelete = item;
                        return;
                    }
                }
            });
        }

        private void OpenNameDialog()
        {
            Find.WindowStack.Add(new Dialog_EquipmentSetName(
                OrderedWorkText.T("OrderedWork.Equipment.Editor.NewTitle", "New equipment set"),
                "New equipment",
                delegate(string name)
                {
                    EquipmentSet created = new EquipmentSet { Name = name, Role = EquipmentSetRole.Principal };
                    if (EquipmentSetStore.Save(created))
                    {
                        onLoad(created);
                        Close();
                    }
                },
                IsUniqueName));
        }

        private void Load(EquipmentSet item)
        {
            onLoad(item);
            Close();
        }

        private void DrawDeleteConfirmation(Rect area)
        {
            UiRows rows = new UiRows(area, 24f, 6f);
            Widgets.Label(rows.Next(), OrderedWorkText.T("OrderedWork.Equipment.Editor.DeleteConfirm", "Delete this set?"));
            Rect buttons = rows.Next(28f);
            Rect[] cells = UiFlex.Columns(buttons, 8f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], OrderedWorkText.T("OrderedWork.Common.ConfirmDelete", "Confirm delete"), true, null, UiColors.ConfirmDanger))
            {
                EquipmentSet? deleting = pendingDelete;
                if (deleting != null)
                {
                    EquipmentSetStore.Delete(deleting.Id);
                }
                pendingDelete = null;
                sets = EquipmentSetStore.LoadAll();
            }
            if (UiButtonBar.Button(cells[1], OrderedWorkText.T("OrderedWork.Common.Cancel", "Cancel")))
            {
                pendingDelete = null;
            }
        }

        private bool IsUniqueName(string name)
        {
            return !EquipmentSetStore.LoadAll().Any(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
