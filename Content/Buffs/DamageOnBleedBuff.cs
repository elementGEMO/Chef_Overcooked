using UnityEngine;
using RoR2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using R2API;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine.Rendering;

namespace ChefOvercooked;
using static OverlayHelper;

public class DamageOnBleedBuff : BuffBase
{
    private static Material ActiveOverlay;
    protected override string Name => "DamageOnBleed";
    public static BuffDef BuffDef;
    protected override Sprite IconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texDamageOnBleed");
    protected override Color Color => new Color(0.910f, 0.506f, 0.239f);
    protected override bool IsHidden => false;
    protected override bool IsStackable => true;

    protected override void Initialize()
    {
        BuffDef = Value;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;
        IL.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;

        IL.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        IL.RoR2.CharacterModel.UpdateOverlayStates += CharacterModel_UpdateOverlayStates;

        CreateOverlay();
    }
    private void CreateOverlay()
    {
        Texture2D tempRamp  = Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_ColorRamps.texRampDroneFire_png).WaitForCompletion();
        ActiveOverlay       = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC2_Chef_Buffs.matChefOiledDebuffOverlay_mat).WaitForCompletion());

        ActiveOverlay.SetInt("_SrcBlend", (int) BlendMode.One);
        ActiveOverlay.SetInt("_DstBlend", (int) BlendMode.One);

        ActiveOverlay.SetFloat("_FresnelPower", 0.8f);
        ActiveOverlay.SetFloat("_AlphaBoost", 0.5f);
        ActiveOverlay.SetFloat("_AlphaBias", 1f);

        ActiveOverlay.SetColor("_TintColor", new Color(1, 0.439f, 0));
        ActiveOverlay.SetTexture("_RemapTex", tempRamp);
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        args.baseDamageAdd += sender.GetBuffCount(BuffDef) * 0.15f;
    }
    private void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        if (NetworkServer.active && buffDef == BuffDef)
        {
            int itemCount = self.inventory ? self.inventory.GetItemCountEffective(PrimitiveClawsItem.ItemDef) : 0;
            int maxBuffs = 3 + 2 * (itemCount - 1);

            int buffCount = 0;
            int lastBuffIndex = -1;
            float timeIncrement = 999f;

            for (int i = 0; i < self.timedBuffs.Count; i++)
            {
                CharacterBody.TimedBuff timedBuff = self.timedBuffs[i];
                if (timedBuff.buffIndex == BuffDef.buffIndex)
                {
                    buffCount++;
                    if (timedBuff.timer < timeIncrement)
                    {
                        lastBuffIndex = i;
                        timeIncrement = timedBuff.timer;
                    }
                }
            }

            if (buffCount < maxBuffs)
            {
                self.timedBuffs.Add(new CharacterBody.TimedBuff
                {
                    buffIndex = BuffDef.buffIndex,
                    timer = duration,
                    totalDuration = duration,
                });
                self.AddBuff(buffDef.buffIndex);
            }
            else if (lastBuffIndex != -1)
            {
                self.timedBuffs[lastBuffIndex].timer = duration;
                self.timedBuffs[lastBuffIndex].totalDuration = duration;
            }

            return;
        }

        orig(self, buffDef, duration);
    }

    private void GlobalEventManager_ProcessHitEnemy(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(
            x => x.MatchLdfld(typeof(DamageInfo), nameof(DamageInfo.procCoefficient)),
            x => x.MatchLdloc(out _),
            x => x.MatchCallvirt(typeof(CharacterBody), "get_bleedChance")
        ))
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdfld(typeof(DamageInfo), nameof(DamageInfo.procChainMask)),
                x => x.MatchStloc(out _)
            ))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_1);

                cursor.EmitDelegate<Action<DamageInfo>>(report =>
                {
                    CharacterBody attackerBody = report.attacker ? report.attacker.GetComponent<CharacterBody>() : null;
                    int itemCount = attackerBody.inventory ? attackerBody.inventory.GetItemCountEffective(PrimitiveClawsItem.ItemDef) : 0;

                    if (itemCount > 0) attackerBody.AddTimedBuff(BuffDef, 3f * report.procCoefficient);
                });
            }
            else Log.Error(BuffDef.name + "_PROCESSHITENEMY failed to ILHook #2");
        }
        else Log.Error(BuffDef.name + "_PROCESSHITENEMY failed to ILHook #1");
    }
    private void CharacterModel_UpdateOverlays(ILContext il)
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
                    AddOverlay(model, ActiveOverlay);
                }
            });
        }
        else Log.Error(BuffDef.name + "_UPDATEOVERLAYS failed to ILHook");
    }
    private void CharacterModel_UpdateOverlayStates(ILContext il)
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
