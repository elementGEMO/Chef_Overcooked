using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;

namespace ChefOvercooked;
public class StalkBuff : BuffBase
{
    protected override string Name => "Stalk";
    public static BuffDef BuffDef;

    protected override Sprite IconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texStalkIcon");

    protected override bool IgnoreGrowthNectar => false;
    protected override bool IsStackable => true;

    private static Material OverlayMat;
    private static Material InvisMat;

    protected override void Initialize()
    {
        BuffDef = Value;

        CreateOverlay();

        IL.RoR2.CharacterModel.UpdateOverlays += (il) => OverlayHelper.UpdateOverlayHook(new(il), BuffDef, [OverlayMat]);
        IL.RoR2.CharacterModel.UpdateOverlayStates += (il) => OverlayHelper.UpdateOverlayStateHook(new(il), BuffDef);
        IL.RoR2.CharacterModel.UpdateRendererMaterials += CharacterModel_UpdateRendererMaterials;

        CharacterBody.onBuffFirstStackGained += CharacterBody_onBuffFirstStackGained;
    }

    private void CharacterBody_onBuffFirstStackGained(CharacterBody self, BuffDef buff)
    {
        if (buff == Value)
            EntityStateMachine.FindByCustomName(self.gameObject, "Body").SetNextState(new StalkState());
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
    private void CreateOverlay()
    {
        OverlayMat = new(Addressables.LoadAssetAsync<Material>(RoR2_Base_Common.matRevealedEffect_mat).WaitForCompletion());

        OverlayMat.SetTexture("_RemapTex", ChefOverCookedPlugin.Bundle.LoadAsset<Texture2D>("texStalkRamp"));
        OverlayMat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_TiledTextures.texCloudOrganicNormal_png).WaitForCompletion());

        OverlayMat.SetTextureScale("_Cloud1Tex", new Vector2(1f, 0.1f));
        OverlayMat.SetVector("_CutoffScroll", new Vector2(1, 10));

        OverlayMat.SetFloat("_Boost", 2f);

        InvisMat = new(Addressables.LoadAssetAsync<Material>(RoR2_Base_Common.matCloakedEffect_mat).WaitForCompletion());
    }
}
