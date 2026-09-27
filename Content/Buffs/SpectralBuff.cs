using UnityEngine;
using RoR2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine.Rendering;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using R2API;

namespace ChefOvercooked;
using static OverlayHelper;

public class SpectralBuff : BuffBase
{
    private static Material ActiveOverlayOne;
    private static Material ActiveOverlayTwo;
    private static Material ActiveOverlayThree;
    protected override string Name => "Spectral";
    public static BuffDef BuffDef;
    protected override bool IsStackable => true;

    protected override void Initialize()
    {
        BuffDef = Value;

        // I got lazy
        IL.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays_ActiveOne;
        IL.RoR2.CharacterModel.UpdateOverlayStates += CharacterModel_UpdateOverlayStates_ActiveOne;

        IL.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays_ActiveTwo;
        IL.RoR2.CharacterModel.UpdateOverlayStates += CharacterModel_UpdateOverlayStates_ActiveTwo;

        IL.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays_ActiveThree;
        IL.RoR2.CharacterModel.UpdateOverlayStates += CharacterModel_UpdateOverlayStates_ActiveThree;

        On.RoR2.CharacterModel.UpdateOverlays += OnCharacterModel_UpdateOverlays;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

        CreateOverlay();
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        args.baseDamageAdd += Math.Max(0, sender.GetBuffCount(BuffDef) - 1) * PluginConfig.Kebab_Lem_Damage.Value / 100;
    }

    private void OnCharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
    {
        orig(self);

        if (self.body.HasBuff(BuffDef)) self.visibility = VisibilityLevel.Revealed;
    }

    private void CreateOverlay()
    {
        ActiveOverlayOne = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile1_mat).WaitForCompletion());
        ActiveOverlayTwo = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile2_mat).WaitForCompletion());
        ActiveOverlayThree = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile3_mat).WaitForCompletion());
    }
    private void CharacterModel_UpdateOverlays_ActiveOne(ILContext il)
    {
        ILCursor cursor = new(il);

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
                if (model.body.GetBuffCount(BuffDef) > 0)
                {
                    AddOverlay(model, ActiveOverlayOne);
                }
            });
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYS failed to ILHook");
    }
    private void CharacterModel_UpdateOverlayStates_ActiveOne(ILContext il)
    {
        ILCursor cursor = new(il);
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
                    if (model.body.HasBuff(BuffDef)) model.activeOverlays |= 1 << index;
                    return index++;
                });

                cursor.Emit(OpCodes.Stloc, incrementIndex);
            }
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYSTATES failed to ILHook #1");
    }
    private void CharacterModel_UpdateOverlays_ActiveTwo(ILContext il)
    {
        ILCursor cursor = new(il);

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
                if (model.body.GetBuffCount(BuffDef) > 0)
                {
                    AddOverlay(model, ActiveOverlayTwo);
                }
            });
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYS failed to ILHook");
    }
    private void CharacterModel_UpdateOverlayStates_ActiveTwo(ILContext il)
    {
        ILCursor cursor = new(il);
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
                    if (model.body.HasBuff(BuffDef)) model.activeOverlays |= 1 << index;
                    return index++;
                });

                cursor.Emit(OpCodes.Stloc, incrementIndex);
            }
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYSTATES failed to ILHook #1");
    }
    private void CharacterModel_UpdateOverlays_ActiveThree(ILContext il)
    {
        ILCursor cursor = new(il);

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
                if (model.body.GetBuffCount(BuffDef) > 0)
                {
                    AddOverlay(model, ActiveOverlayThree);
                }
            });
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYS failed to ILHook");
    }
    private void CharacterModel_UpdateOverlayStates_ActiveThree(ILContext il)
    {
        ILCursor cursor = new(il);
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
                    if (model.body.HasBuff(BuffDef)) model.activeOverlays |= 1 << index;
                    return index++;
                });

                cursor.Emit(OpCodes.Stloc, incrementIndex);
            }
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYSTATES failed to ILHook #1");
    }
}
