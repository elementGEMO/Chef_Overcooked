using System.Collections.Generic;
using System;
using RoR2;
using R2API;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine.UIElements;

namespace ChefOvercooked;

internal static class HelperLanguage
{
    public static string LanguageAdd(string token, string description)
    {
        LanguageAPI.Add(ChefOverCookedPlugin.TokenPrefix + token, description);
        return ChefOverCookedPlugin.TokenPrefix + token;
    }
}

internal static class HelperFontColor
{
    public static string Style(this string self, FontColor style) => "<style=" + style + ">" + self + "</style>";
    public static string Style(this string self, string color) => "<color=" + color + ">" + self + "</color>";
    public enum FontColor
    {
        cStack,
        cIsDamage,
        cIsHealth,
        cIsUtility,
        cIsHealing,
        cDeath,
        cSub,
        cKeywordName,
        cIsVoid,
        cIsLunar
    };
}

internal static class HelperRender
{
    public static CharacterModel.RendererInfo[] ItemDisplaySetup(GameObject self)
    {
        Renderer[] allRender = self.GetComponentsInChildren<Renderer>();
        List<CharacterModel.RendererInfo> renderInfos = [];

        foreach (Renderer render in allRender)
        {
            renderInfos.Add(new CharacterModel.RendererInfo
            {
                defaultMaterial = render.sharedMaterial,
                renderer = render,
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                ignoreOverlays = false
            });
        }

        return [.. renderInfos];
    }
}

public struct CombinedItemTier
{
    public ItemTierDef ItemTierDef;
    public ItemTier ItemTier;

    public static implicit operator ItemTierDef(CombinedItemTier self) => self.ItemTierDef;
    public static implicit operator ItemTier(CombinedItemTier self) => self.ItemTier;

    public static implicit operator CombinedItemTier(ItemTier itemTier)
    {
        return new CombinedItemTier
        {
            ItemTier = itemTier
        };
    }

    public static implicit operator CombinedItemTier(ItemTierDef itemTierDef)
    {
        return new CombinedItemTier
        {
            ItemTierDef = itemTierDef
        };
    }
}
internal static class SoundHelper
{
    public static NetworkSoundEventDef CreateNetworkSoundDef(string soundName)
    {
        NetworkSoundEventDef soundDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();

        soundDef.eventName = soundName;
        soundDef.name = soundName;

        ContentAddition.AddNetworkSoundEventDef(soundDef);

        return soundDef;
    }
}
internal static class StringHelper
{
    public static string SignVal(this float value) => value >= 0f ? "+" + value : "" + value;
    public static string SignVal(this int value) => value >= 0 ? "+" + value : "" + value;
    public static float RoundVal(float value) => MathF.Round(value, PluginConfig.Round_To.Value);
    public static float RoundVal(float value, int extraPoint) => MathF.Round(value, PluginConfig.Round_To.Value + extraPoint);
    public static string OptText(this string self, bool value) => value ? self : "";
    public static string OptText(this string self, string opt, bool value) => value ? self : opt;
    public static string FuseText(List<string> allStrings) => string.Join("", allStrings);
}
internal static class OverlayHelper
{
    public static void UpdateOverlayHook(this ILCursor cursor, BuffDef buffDef, Material[] overlays)
    {
        if (cursor.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(typeof(CharacterModel), nameof(CharacterModel.body)),
            x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.ClayGoo))
        ))
        {
            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldarg_0);

            cursor.EmitDelegate<Action<CharacterModel>>(model =>
            {
                if (model.body && model.body.GetBuffCount(buffDef) > 0)
                {
                    foreach (Material material in overlays)
                        AddOverlay(model, material);
                }
            });
        }
        else Log.Error(string.Format("{0}_UPDATEOVERLAYHOOK failed to ILHook", buffDef.name));
    }

    public static void UpdateOverlayStateHook(this ILCursor cursor, BuffDef buffDef, int totalOverlays = 1)
    {
        int incrementIndex = -1;

        if (cursor.TryGotoNext(
            x => x.MatchLdcI4(0),
            x => x.MatchStloc(out incrementIndex),
            x => x.MatchLdarg(0),
            x => x.MatchLdloc(incrementIndex)
        ) && incrementIndex != -1)
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(incrementIndex)
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, incrementIndex);

                cursor.EmitDelegate<Func<CharacterModel, int, int>>((model, index) =>
                {
                    if (model.body.HasBuff(buffDef)) model.activeOverlays |= totalOverlays << index;
                    return index++;
                });

                cursor.Emit(OpCodes.Stloc, incrementIndex);
            }
        }
        else Log.Error(string.Format("{0}_UPDATEOVERLAYSTATES failed to ILHook", buffDef.name));
    }
    private static void AddOverlay(CharacterModel model, Material overlayMaterial)
    {
        if (model.activeOverlayCount >= CharacterModel.maxOverlays || !overlayMaterial) return;

        Material[] allOverlays = model.currentOverlays;
        int overlayCount = model.activeOverlayCount;
        model.activeOverlayCount = overlayCount + 1;
        allOverlays[overlayCount] = overlayMaterial;
    }
}