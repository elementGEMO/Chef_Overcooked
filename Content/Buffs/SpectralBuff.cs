using UnityEngine;
using RoR2;
using R2API;
using System;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace ChefOvercooked;
public class SpectralBuff : BuffBase
{
    protected override string Name => "Spectral";
    public static BuffDef BuffDef;

    private static Material OverlayMatOne;
    private static Material OverlayMatTwo;
    private static Material OverlayMatThree;
    private static Material InvisMat = ChefOverCookedPlugin.Bundle.LoadAsset<Material>("matInvis");

    protected override void Initialize()
    {
        BuffDef = Value;

        CreateOverlay();

        IL.RoR2.CharacterModel.UpdateOverlays += (il) => OverlayHelper.UpdateOverlayHook(new(il), BuffDef, [OverlayMatOne, OverlayMatTwo, OverlayMatThree]);
        IL.RoR2.CharacterModel.UpdateOverlayStates += (il) => OverlayHelper.UpdateOverlayStateHook(new(il), BuffDef, 3);
        IL.RoR2.CharacterModel.UpdateRendererMaterials += CharacterModel_UpdateRendererMaterials;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
    }

    private void CreateOverlay()
    {
        OverlayMatOne = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile1_mat).WaitForCompletion());
        OverlayMatTwo = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile2_mat).WaitForCompletion());
        OverlayMatThree = new(Addressables.LoadAssetAsync<Material>(RoR2_DLC3_Items_WyrmOnHit.matWyrmProjectile3_mat).WaitForCompletion());
    }
    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (!sender.HasBuff(BuffDef) || !sender.master)
            return;

        args.attackSpeedMultAdd += 0.15f;
        args.moveSpeedMultAdd += 0.14f;

        Deployable deployable       = sender.master.GetComponent<Deployable>();
        CharacterMaster ownerMaster = deployable ? deployable.ownerMaster : null;
        CharacterBody masterBody    = ownerMaster ? ownerMaster.GetBody() : null;

        if (!ownerMaster)
            return;

        args.baseDamageAdd += masterBody.damage * PluginConfig.Kebab_Base_Damage.Value / 100f;
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
}
