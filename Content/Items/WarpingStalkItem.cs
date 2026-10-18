using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine.AddressableAssets;
using UnityEngine;
using R2API;
using RoR2;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class WarpingStalkItem : ItemBase
{
    protected override string Name => "WarpingStalk";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.Tier2;
    protected override ItemTag[] Tags => [
        ItemTag.Utility,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary,
        ItemTag.BrotherBlacklist
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("warpingStalkModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texWarpingStalkIcon");
    protected override string PickupText => "Chance to nullify incoming damage, and fade briefly into the Red Plane.";
    protected override string Description => FuseText([
        string.Format("{0}% ".Style(FontColor.cIsUtility) + "chance to " + "nullify incoming damage ".Style(FontColor.cIsHealing),
            RoundVal(PluginConfig.Stalk_Nullify_Chance.Value)),
        
        string.Format("and become " + "intangible".Style(FontColor.cIsUtility) + " for " + "{0} ".Style(FontColor.cIsUtility) + "({1} per stack) ".Style(FontColor.cStack).OptText(PluginConfig.Stalk_Duration_Stack.Value > 0) + "seconds".Style(FontColor.cIsUtility),
            RoundVal(PluginConfig.Stalk_Base_Duration.Value), RoundVal(PluginConfig.Stalk_Duration_Stack.Value).SignVal()),
        
        ", causing " + "ALL attacks ".Style(FontColor.cIsDamage) + "to pass through you.",

        " Unaffected by luck".Style(FontColor.cIsUtility).OptText(!PluginConfig.Stalk_Luck_Affected.Value) + ".".OptText(!PluginConfig.Stalk_Luck_Affected.Value)
    ]);
    protected override string DisplayName => "Warping Stalk";

    private static EffectDef NullEffect;

    protected override void Initialize()
    {
        ItemDef = Value;

        CreateEffects();

        On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
    }

    private static void CreateEffects()
    {
        GameObject prefab = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Bear.BearProc_prefab).WaitForCompletion().InstantiateClone("NullifyEffect");

        EffectComponent effectComponent = prefab.GetComponent<EffectComponent>();
        effectComponent.soundName = null;

        RoR2.UI.LanguageTextMeshController text = prefab.transform
            .Find("TextCamScaler")
            .Find("TextRiser")
            .Find("TextMeshPro")
            .GetComponent<RoR2.UI.LanguageTextMeshController>();

        text.token = HelperLanguage.LanguageAdd("WARPING_BLOCK", "NULLIFIED!");

        Object.Destroy(prefab.transform.Find("Fluff").gameObject);

        NullEffect = new()
        {
            prefab = prefab,
            prefabName = "NullifyEffect",
            prefabEffectComponent = effectComponent
        };

        ContentAddition.AddEffect(NullEffect.prefab);
    } 

    private void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
    {
        float luck      = (PluginConfig.Stalk_Luck_Affected.Value && self.body.master) ? self.body.master.luck : 0f;
        int itemCount   = self.body.inventory ? self.body.inventory.GetItemCountEffective(ItemDef): 0;
        bool canStalk   = self.body.master ? Util.CheckRoll(PluginConfig.Stalk_Nullify_Chance.Value, luck, self.body.master) && itemCount > 0 : false;

        if (!self.body.HasBuff(StalkBuff.BuffDef) && canStalk)
        {
            EffectData hitEffect = new() {
                origin = damageInfo.position,
                rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : Random.onUnitSphere)
            };

            EffectManager.SpawnEffect(NullEffect.prefab, hitEffect, true);

            float baseDuration = PluginConfig.Stalk_Base_Duration.Value + PluginConfig.Stalk_Base_Duration.Value * (itemCount - 1);
            float procDuration = Mathf.Lerp(Mathf.Max(baseDuration * 0.20f, 1), baseDuration, damageInfo.procCoefficient);

            for (int i = 0; i < Mathf.CeilToInt(procDuration); i++)
                self.body.AddTimedBuff(StalkBuff.BuffDef, procDuration / Mathf.CeilToInt(procDuration) * i);
        }
        else orig(self, damageInfo);
    }

    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 3f;
        modelParam.maxDistance = 12f;
    }
}