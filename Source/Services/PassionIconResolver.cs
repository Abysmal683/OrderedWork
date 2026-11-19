using System;
using RimWorld;
using UnityEngine;

namespace OrderedWork.Services
{
    /// <summary>
    /// Optional VSE/Alpha Skills textures. The caller owns fallback drawing when a
    /// texture is absent.
    /// </summary>
    public static class PassionIconResolver
    {
        public static Texture2D IconOf(Passion passion)
        {
            return TextureOf(passion, "Icon");
        }

        public static Texture2D WorkBoxIconOf(Passion passion)
        {
            return TextureOf(passion, "WorkBoxIcon");
        }

        private static Texture2D TextureOf(Passion passion, string memberName)
        {
            object def = PassionRegistry.GetVseDef(passion);
            if (def == null)
            {
                return null;
            }
            try
            {
                return PassionRegistry.GetMember<object>(def, memberName) as Texture2D;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}