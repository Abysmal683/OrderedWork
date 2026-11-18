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
            list.Label("Global JSON profiles (pending edits apply on restart):");
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
                profileStatus = "Could not list profiles: " + exception.Message;
            }

            Rect selector = list.GetRect(28f);
            if (Widgets.ButtonText(selector, "Active profile: " + activeProfileName + "  ▼"))
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
                        profileStatus = "Selected " + captured + ". Apply on next startup.";
                    }));
                }
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            list.Gap(6f);
            newProfileName = UiFieldRow.Text(list.GetRect(26f), "Save as", newProfileName, 84f);
            list.Gap(6f);


            if (UiButtonBar.Button(list.GetRect(28f), "Save current as profile", true, "Save the active profile as JSON."))
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
                    profileStatus = "Saved " + profile + ".json";
                }
                else
                {
                    profileStatus = "Failed to save " + profile + ".json";
                }
            list.Gap(4f);
            }
            if (UiButtonBar.Button(list.GetRect(28f), "Write pending for active profile", true, "Write pending edits for the active profile."))
            {
                WorkConfigProfile captured = WorkConfigProfileBuilder.Capture(Settings, activeProfileName);
                profileStatus = WorkConfigStore.SavePending(captured)
                    ? "Wrote " + activeProfileName + ".pending.json. Apply on next startup."
                    : "Failed to write pending profile.";
            }

            list.Label("Reference: " + WorkConfigStore.ReferencePath);
            if (!string.IsNullOrEmpty(profileStatus))
            {
                list.Label(profileStatus);
            }
            list.Gap(8f);
        }
    }
}