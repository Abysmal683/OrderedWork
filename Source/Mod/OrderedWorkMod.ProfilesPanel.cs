using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private void DrawProfilePanel(Listing_Standard list)
        {
            list.Gap(8f);
            list.Label(ConfigText("OrderedWork.Config.Profiles.Info", "Global JSON profiles (pending edits apply on restart):"));
            if (!profileNameInitialized)
            {
                activeProfileName = WorkConfigStore.LoadSelectedProfileName();
                newProfileName = activeProfileName;
                profileNameInitialized = true;
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
                profileStatus = string.Format(ConfigText("OrderedWork.Config.Profiles.ListError", "Could not list profiles: {0}"), exception.Message);
            }

            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, string.Format(ConfigText("OrderedWork.Config.Profiles.Active", "Active profile: {0}  ▼"), activeProfileName)))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string profile in profiles)
                {
                    string captured = profile;
                    options.Add(new FloatMenuOption(profile, delegate
                    {
                        activeProfileName = captured;
                        newProfileName = captured;
                        WorkConfigStore.SaveSelectedProfileName(captured);
                        profileStatus = string.Format(ConfigText("OrderedWork.Config.Profiles.Selected", "Selected {0}. Apply on next startup."), captured);
                    }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            list.Gap(6f);
            newProfileName = UiFieldRow.Text(list.GetRect(26f), ConfigText("OrderedWork.Config.Profiles.SaveAs", "Save as"), newProfileName, 84f);
            list.Gap(6f);


            if (UiButtonBar.Button(list.GetRect(28f), ConfigText("OrderedWork.Config.Profiles.SaveCurrent", "Save current as profile"), true, ConfigText("OrderedWork.Config.Profiles.SaveCurrentTip", "Save the active profile as JSON.")))
            {
                string profile = string.IsNullOrWhiteSpace(newProfileName) ? activeProfileName : newProfileName.Trim();
                if (string.IsNullOrWhiteSpace(profile))
                {
                    profile = "default";
                }
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, profile);
                if (WorkConfigStore.SaveProfile(captured, true))
                {
                    activeProfileName = profile;
                    WorkConfigStore.SaveSelectedProfileName(profile);
                    PersistLivePending();
                    profileStatus = string.Format(ConfigText("OrderedWork.Config.Profiles.Saved", "Saved {0}.json"), profile);
                }
                else
                {
                    profileStatus = string.Format(ConfigText("OrderedWork.Config.Profiles.FailedSave", "Failed to save {0}.json"), profile);
                }
            list.Gap(4f);
            }
            if (UiButtonBar.Button(list.GetRect(28f), ConfigText("OrderedWork.Config.Profiles.WritePending", "Write pending for active profile"), true, ConfigText("OrderedWork.Config.Profiles.WritePendingTip", "Write pending edits for the active profile.")))
            {
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
                profileStatus = WorkConfigStore.SavePending(captured)
                    ? string.Format(ConfigText("OrderedWork.Config.Profiles.WrotePending", "Wrote {0}.pending.json. Apply on next startup."), activeProfileName)
                    : ConfigText("OrderedWork.Config.Profiles.FailedPending", "Failed to write pending profile.");
            }

            list.Label(string.Format(ConfigText("OrderedWork.Config.Profiles.Reference", "Reference: {0}"), WorkConfigStore.ReferencePath));
            if (!string.IsNullOrEmpty(profileStatus))
            {
                list.Label(profileStatus);
            }
            list.Gap(8f);
        }
    }
}