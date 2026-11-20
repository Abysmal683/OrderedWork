using UnityEngine;

namespace OrderedWork.Services
{
    internal static class ClothingIconResolver
    {
        public static Texture2D? IconOf(string? defName)
        {
            return ClothingInfoService.Find(defName)?.Icon;
        }
    }
}