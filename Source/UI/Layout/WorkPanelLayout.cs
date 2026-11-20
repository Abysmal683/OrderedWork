#nullable disable
using UnityEngine;

namespace OrderedWork.UI.Layout
{
    /// <summary>
    /// Single source of truth for the Work panel geometry. Draw and input use these rects.
    /// </summary>
    public sealed class WorkPanelRects
    {
        public bool Stacked;
        public Rect Header;
        public Rect Body;
        public Rect WorkTypes;
        public Rect Givers;
        public Rect Metadata;
    }

    public static class WorkPanelLayout
    {
        private const float HeaderHeight = 32f;
        private const float Gap = 8f;
        private const float StackThreshold = 940f;

        public static WorkPanelRects Build(Rect area, bool giversExpanded)
        {
            WorkPanelRects result = new WorkPanelRects();
            result.Header = new Rect(area.x, area.y, area.width, HeaderHeight);
            result.Body = new Rect(area.x, result.Header.yMax + 4f, area.width,
                Mathf.Max(0f, area.height - HeaderHeight - 4f));
            result.Stacked = result.Body.width < StackThreshold;

            if (result.Stacked)
            {
                result.WorkTypes = new Rect(result.Body.x, result.Body.y, result.Body.width, 330f);
                result.Givers = new Rect(result.Body.x, result.WorkTypes.yMax + Gap, result.Body.width, 260f);
                result.Metadata = new Rect(result.Body.x, result.Givers.yMax + Gap, result.Body.width, 200f);
                return result;
            }

            float leftWidth = Mathf.Clamp(result.Body.width * 0.31f, 280f, 390f);
            float middleWidth = Mathf.Clamp(result.Body.width * 0.34f, 300f, 430f);
            float metadataWidth = Mathf.Max(260f,
                result.Body.width - leftWidth - middleWidth - Gap * 2f);

            result.WorkTypes = new Rect(result.Body.x, result.Body.y, leftWidth, result.Body.height);
            result.Givers = new Rect(result.WorkTypes.xMax + Gap, result.Body.y, middleWidth, result.Body.height);
            result.Metadata = new Rect(result.Givers.xMax + Gap, result.Body.y, metadataWidth, result.Body.height);
            return result;
        }
    }
}
