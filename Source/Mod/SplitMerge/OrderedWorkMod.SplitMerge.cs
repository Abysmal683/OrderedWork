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
        internal WorkConfigProfile? LoadEditingProfile()
        {
            WorkConfigProfile? pending = WorkConfigStore.LoadPending(activeProfileName);
            if (pending != null)
            {
                return pending;
            }
            WorkConfigLoadResult active = WorkConfigStore.LoadActiveWithFallback(activeProfileName);
            return active != null ? active.Profile : null;
        }


    }
}