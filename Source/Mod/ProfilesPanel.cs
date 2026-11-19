using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrderedWork.Config;
using OrderedWork.UI.Layout;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrderedWork
{
    /// <summary>
    /// Profile list and save/pending controls. The panel owns its name/initialization
    /// state; the active profile and shared status are passed by the settings host.
    /// </summary>
    internal sealed class ProfilesPanel
    {
        private bool initialized;
        private string newProfileName = "";

        internal void Draw(Listing_Standard list, OrderedWorkSettings settings, string activeProfile,
            Action<string> setActiveProfile, string status, Action<string> setStatus, Action persistPending)
        {
            list.Gap(8f);
            list.Label(OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.Info", "Global JSON profiles (pending edits apply on restart):"));
            if (!initialized)
            {
                newProfileName = activeProfile;
                initialized = true;
            }

            List<string> profiles = new List<string>();
            try
            {
                Directory.CreateDirectory(WorkConfigStore.ProfilesDirectory);
                profiles = Directory.GetFiles(WorkConfigStore.ProfilesDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !n.EndsWith(".pending", StringComparison.Ordinal))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToList();
            }
            catch (Exception exception)
            {
                setStatus(string.Format(
                    OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.ListError", "Could not list profiles: {0}"),
                    exception.Message));
            }

            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, string.Format(
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.Active", "Active profile: {0}  ▼"),
                activeProfile)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string profile in profiles)
                {
                    string captured = profile;
                    options.Add(new FloatMenuOption(profile, delegate
                    {
                        setActiveProfile(captured);
                        newProfileName = captured;
                        WorkConfigStore.SaveSelectedProfileName(captured);
                        setStatus(string.Format(
                            OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.Selected", "Selected {0}. Apply on next startup."),
                            captured));
                    }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            list.Gap(6f);
            newProfileName = UiFieldRow.Text(list.GetRect(26f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.SaveAs", "Save as"),
                newProfileName, 84f);
            list.Gap(6f);

            if (UiButtonBar.Button(list.GetRect(28f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.SaveCurrent", "Save current as profile"),
                true,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.SaveCurrentTip", "Save the active profile as JSON.")))
            {
                string profile = string.IsNullOrWhiteSpace(newProfileName) ? activeProfile : newProfileName.Trim();
                if (string.IsNullOrWhiteSpace(profile))
                {
                    profile = "default";
                }
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(settings, profile);
                if (WorkConfigStore.SaveProfile(captured, true))
                {
                    setActiveProfile(profile);
                    WorkConfigStore.SaveSelectedProfileName(profile);
                    persistPending();
                    setStatus(string.Format(
                        OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.Saved", "Saved {0}.json"),
                        profile));
                }
                else
                {
                    setStatus(string.Format(
                        OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.FailedSave", "Failed to save {0}.json"),
                        profile));
                }
                list.Gap(4f);
            }
            if (UiButtonBar.Button(list.GetRect(28f),
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.WritePending", "Write pending for active profile"),
                true,
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.WritePendingTip", "Write pending edits for the active profile.")))
            {
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(settings, activeProfile);
                setStatus(WorkConfigStore.SavePending(captured)
                    ? string.Format(
                        OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.WrotePending", "Wrote {0}.pending.json. Apply on next startup."),
                        activeProfile)
                    : OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.FailedPending", "Failed to write pending profile."));
            }

            list.Label(string.Format(
                OrderedWork.Localization.OrderedWorkText.T("OrderedWork.Config.Profiles.Reference", "Reference: {0}"),
                WorkConfigStore.ReferencePath));
            if (!string.IsNullOrEmpty(status))
            {
                list.Label(status);
            }
            list.Gap(8f);
        }
    }
}