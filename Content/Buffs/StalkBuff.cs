using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using UnityEngine.Networking;

namespace ChefOvercooked;
public class StalkBuff : BuffBase
{
    protected override string Name => "Stalk";
    public static BuffDef BuffDef;

    protected override Sprite IconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texStalkIcon");

    protected override bool IgnoreGrowthNectar => false;
    protected override bool IsStackable => true;

    private static EffectDef StalkEffect;
    private static Material OverlayMat;
    private static Material InvisMat = ChefOverCookedPlugin.Bundle.LoadAsset<Material>("matInvis");

    protected override void Initialize()
    {
        BuffDef = Value;

        CreateEffects();

        IL.RoR2.CharacterModel.UpdateOverlays += (il) => OverlayHelper.UpdateOverlayHook(new(il), BuffDef, [OverlayMat]);
        IL.RoR2.CharacterModel.UpdateOverlayStates += (il) => OverlayHelper.UpdateOverlayStateHook(new(il), BuffDef);
        IL.RoR2.CharacterModel.UpdateRendererMaterials += CharacterModel_UpdateRendererMaterials;

        CharacterBody.onBuffFirstStackGained += CharacterBody_onBuffFirstStackGained;
        CharacterBody.onBuffFinalStackLost += CharacterBody_onBuffFinalStackLost;
    }

    private void CharacterBody_onBuffFirstStackGained(CharacterBody self, BuffDef buff)
    {
        if (!NetworkServer.active)
            return;

        if (!self || buff != Value)
            return;

        BlinkParticle(self);

        if (self.hurtBoxGroup)
            self.hurtBoxGroup.hurtBoxesDeactivatorCounter++;
    }
    private void CharacterBody_onBuffFinalStackLost(CharacterBody self, BuffDef buff)
    {
        if (!NetworkServer.active)
            return;

        if (!self || buff != Value)
            return;

        BlinkParticle(self);

        if (self.hurtBoxGroup)
            self.hurtBoxGroup.hurtBoxesDeactivatorCounter--;
    }
    private void BlinkParticle(CharacterBody self)
    {
        Util.PlaySound("Play_voidBarnacle_m1_impact", self.gameObject);
        EffectManager.SpawnEffect(StalkEffect.prefab, new EffectData()
        {
            origin = Util.GetCorePosition(self),
            rotation = UnityEngine.Random.rotation
        }, true);
    }
    private void CharacterModel_UpdateRendererMaterials(ILContext il)
    {
        ILCursor cursor = new(il);

        int matIndex = -1;

        cursor.TryGotoNext(
            x => x.MatchLdnull(),
            x => x.MatchStloc(out matIndex)
        );

        if (matIndex != -1 && cursor.TryGotoNext(
            MoveType.AfterLabel,
            x => x.MatchBr(out _),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(typeof(CharacterModel), nameof(CharacterModel.particleMaterialOverride))
        ))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, matIndex);

            cursor.EmitDelegate<Func<CharacterModel, Material, Material>>((self, material) =>
            {
                if (self.body && self.body.HasBuff(BuffDef))
                    return InvisMat;

                return material;
            });

            cursor.Emit(OpCodes.Stloc, matIndex);
        }
        else Log.Error("STALKBUFF_MATERIAL failed to ILHook");
    }
    private void CreateEffects()
    {
        OverlayMat = new(Addressables.LoadAssetAsync<Material>(RoR2_Base_Common.matRevealedEffect_mat).WaitForCompletion());

        OverlayMat.SetTexture("_RemapTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture2D>("texStalkRamp"));
        OverlayMat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_TiledTextures.texCloudOrganicNormal_png).WaitForCompletion());

        OverlayMat.SetTextureScale("_Cloud1Tex", new Vector2(1f, 0.1f));
        OverlayMat.SetVector("_CutoffScroll", new Vector2(1, 10));

        OverlayMat.SetFloat("_Boost", 2f);

        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Imp.ImpBlinkEffect_prefab).WaitForCompletion().InstantiateClone("StalkBlinkEffect");

        Transform particles = effectPrefab.transform.Find("Particles");
        UnityEngine.Object.Destroy(particles.Find("Dash").gameObject);

        particles.Find("NoiseTrails").localScale = Vector3.one * 2f;

        StalkEffect = new()
        {
            prefab = effectPrefab,
            prefabName = "StalkBlinkEffect",
            prefabEffectComponent = effectPrefab.GetComponent<EffectComponent>()
        };

        ContentAddition.AddEffect(StalkEffect.prefab);
    }
}
