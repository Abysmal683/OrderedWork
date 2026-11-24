using System;
using OrderedWork.Localization;
using OrderedWork.UI.Layout;
using UnityEngine;
using Verse;

namespace OrderedWork.UI
{
    /// <summary>
    /// Name-only dialog for New and Save As. Names never edit inline in the editor.
    /// </summary>
    public sealed class Dialog_EquipmentSetName : Window
    {
        private readonly string title;
        private readonly Action<string> onAccept;
        private readonly Func<string, bool> isUnique;
        private string name;
        private string error = "";

        public Dialog_EquipmentSetName(string title, string initialName, Action<string> onAccept, Func<string, bool> isUnique)
        {
            this.title = title;
            this.onAccept = onAccept;
            this.isUnique = isUnique;
            name = initialName ?? "";
            doCloseX = true;
            doCloseButton = false;
            resizeable = false;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(420f, 150f);

        public override void DoWindowContents(Rect inRect)
        {
            UiRows rows = new UiRows(UiInset.All(inRect, 10f), 28f, 8f);
            Rect titleRect = rows.Next();
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;

            name = UiFieldRow.Text(rows.Next(), OrderedWorkText.T("OrderedWork.Equipment.Editor.NameLabel", "Name"), name, 70f);
            if (!string.IsNullOrEmpty(error))
            {
                Widgets.Label(rows.Next(20f), error);
            }

            Rect buttons = rows.Next(28f);
            Rect[] cells = UiFlex.Columns(buttons, 8f, 1f, 1f);
            if (UiButtonBar.Button(cells[0], OrderedWorkText.T("OrderedWork.Common.Save", "Save"), !string.IsNullOrWhiteSpace(name)))
            {
                string trimmed = name.Trim();
                if (!isUnique(trimmed))
                {
                    error = OrderedWorkText.T("OrderedWork.Equipment.Editor.NameExists", "That name already exists.");
                    return;
                }
                onAccept(trimmed);
                Close();
            }
            if (UiButtonBar.Button(cells[1], OrderedWorkText.T("OrderedWork.Common.Cancel", "Cancel")))
            {
                Close();
            }
        }
    }
}
