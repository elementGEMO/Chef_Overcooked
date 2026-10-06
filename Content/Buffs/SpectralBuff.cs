using UnityEngine;
using RoR2;
using R2API;
using System;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;

namespace ChefOvercooked;
public class SpectralBuff : BuffBase
{
    protected override string Name => "Spectral";
    public static BuffDef BuffDef;
    protected override bool IsStackable => true;

    private static Material OverlayMatOne;
    private static Material OverlayMatTwo;
    private static Material OverlayMatThree;

    protected override void Initialize()
    {
        BuffDef = Value;

        CreateOverlay();

        IL.RoR2.CharacterModel.UpdateOverlays += (il) => OverlayHelper.UpdateOverlayHook(new(il), BuffDef, [OverlayMatOne, OverlayMatTwo, OverlayMatThree]);
        IL.RoR2.CharacterModel.UpdateOverlayStates += (il) => OverlayHelper.UpdateOverlayStateHook(new(il), BuffDef, 3);

        On.RoR2.CharacterModel.UpdateOverlays += OnCharacterModel_UpdateOverlays;
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
        args.baseDamageAdd += Math.Max(0, sender.GetBuffCount(BuffDef) - 1) * PluginConfig.Kebab_Lem_Damage.Value / 100;
    }

    private void OnCharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
    {
        orig(self);

        if (self.body)
        {
            if (self.body.HasBuff(BuffDef) && self.visibility != VisibilityLevel.Invisible)
                self.visibility = VisibilityLevel.Revealed;
        }

        //if (self.body.HasBuff(BuffDef)) self.visibility = VisibilityLevel.Revealed;
    }
}
