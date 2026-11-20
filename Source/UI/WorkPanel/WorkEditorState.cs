using System;
using System.Collections.Generic;

namespace OrderedWork.UI
{
    /// <summary>Mutable per-pawn Work editor state, separate from layout and widgets.</summary>
    public sealed class WorkEditorState
    {
        public List<string> WorkDraft = new List<string>();
        public string? SelectedWorkType;
        public bool GiversExpanded = true;
        public readonly Dictionary<string, List<string>> GiverDrafts =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        public readonly Dictionary<string, List<string>> GiverBaselines =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
    }
}