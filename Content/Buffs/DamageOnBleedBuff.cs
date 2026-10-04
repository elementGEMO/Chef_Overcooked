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

public class DamageOnBleedBuff : BuffBase
{
    protected override string Name => "DamageOnBleed";
    public static BuffDef BuffDef;
    protected override Sprite IconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texDamageOnBleed");
    protected override Color Color => new (0.910f, 0.506f, 0.239f);
    protected override bool IsHidden => false;
    protected override bool IsStackable => true;

    private static Material MatOverlay;

    protected override void Initialize()
    {
        BuffDef = Value;

        CreateOverlay();

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

        DotController.onDotInflictedServerGlobal += DotController_onDotInflictedServerGlobal;
        On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;

        IL.RoR2.CharacterModel.UpdateOverlays += (il) => OverlayHelper.UpdateOverlayHook(new(il), BuffDef, [MatOverlay]);
        IL.RoR2.CharacterModel.UpdateOverlayStates += (il) => OverlayHelper.UpdateOverlayStateHook(new(il), BuffDef);
    }

    private void CreateOverlay()
    {
        Texture2D tempRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_ColorRamps.texRampDroneFire_png).WaitForCompletion();
        MatOverlay = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC2_Chef_Buffs.matChefOiledDebuffOverlay_mat).WaitForCompletion());

        MatOverlay.SetInt("_SrcBlend", (int)BlendMode.One);
        MatOverlay.SetInt("_DstBlend", (int)BlendMode.One);

        MatOverlay.SetFloat("_FresnelPower", 0.8f);
        MatOverlay.SetFloat("_AlphaBoost", 0.5f);
        MatOverlay.SetFloat("_AlphaBias", 1f);

        MatOverlay.SetColor("_TintColor", new Color(1, 0.439f, 0));
        MatOverlay.SetTexture("_RemapTex", tempRamp);
    }
    private void DotController_onDotInflictedServerGlobal(DotController dotController, ref InflictDotInfo inflictDotInfo)
    {
        if (inflictDotInfo.dotIndex != DotController.DotIndex.Bleed && inflictDotInfo.dotIndex != DotController.DotIndex.SuperBleed) return;

        if (inflictDotInfo.attackerObject)
        {
            GameObject attackerObject   = inflictDotInfo.attackerObject;
            CharacterBody attackerBody  = attackerObject.GetComponent<CharacterBody>();
            Inventory inventory         = attackerBody ? attackerBody.inventory : null;
            int itemCount               = inventory ? inventory.GetItemCountEffective(PrimitiveClawsItem.ItemDef) : 0;

            if (itemCount > 0)
                attackerBody.AddTimedBuff(BuffDef, inflictDotInfo.duration);
        }
    }
    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        args.baseDamageAdd += sender.GetBuffCount(BuffDef) * PluginConfig.Claw_Damage_Stack.Value / 100f;
    }
    private void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        if (NetworkServer.active && buffDef == BuffDef)
        {
            int itemCount = self.inventory ? self.inventory.GetItemCountEffective(PrimitiveClawsItem.ItemDef) : 0;
            int maxBuffs = PluginConfig.Claw_Stack_Cap.Value + PluginConfig.Claw_Stack_Increase.Value * (itemCount - 1);

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
        }
        else
        {
            orig(self, buffDef, duration);
        }
    }
}
