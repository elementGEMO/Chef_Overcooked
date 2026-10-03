using EntityStates;
using EntityStates.BrotherMonster;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Items;
using RoR2BepInExPack.GameAssetPathsBetter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class GlowingJellyPastaItem : ItemBase
{
    protected override string Name => "GlowingJellyPasta";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.Utility,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("glowingJellyPastaModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texGlowingJellyPastaIcon");
    protected override string PickupText => string.Format("Reduce skill cooldowns by {0} second" + "s".OptText(PluginConfig.Jelly_Cooldown_Base.Value > 1) + ". Activating your Utility skill releases a stunning shockwave. Leaves an electrifying aftertaste.",
        RoundVal(PluginConfig.Jelly_Cooldown_Base.Value));
    protected override string Description => FuseText([
        string.Format("Reduce " + "skill cooldowns ".Style(FontColor.cIsUtility) + "by " + "{0} ".Style(FontColor.cIsUtility) + "({1} per stack) ".Style(FontColor.cStack) + "second" + "s".OptText(PluginConfig.Jelly_Cooldown_Base.Value > 1) + ". ",
            RoundVal(PluginConfig.Jelly_Cooldown_Base.Value), RoundVal(PluginConfig.Jelly_Cooldown_Stack.Value, 1).SignVal()),

        string.Format("Activating your " + "Utility Skill ".Style(FontColor.cIsUtility) + "releases a shockwave that " + "stuns ".Style(FontColor.cIsUtility) + "enemies for " + "{0} ".Style(FontColor.cIsUtility) + "seconds.",
            RoundVal(PluginConfig.Jelly_Stun_Duration.Value))
    ]);
    protected override string DisplayName => "Glowing Jelly Pasta";
    private static EffectDef ShockwaveEffect;
    protected override void Initialize()
    {
        ItemDef = Value;

        CreateEffect();

        On.RoR2.CharacterBody.OnSkillActivated += CharacterBody_OnSkillActivated;
        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "AttackSpeedAndMoveSpeed", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "BarrierOnCooldown", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "FlatHealth", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "HealWhileSafe", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "HealingPotion", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "Mushroom", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "SpeedBoostPickup", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "SprintBonus", Name);
        RecipeCatalogChef.AddRecipe("StunChanceOnHit", "Tooth", Name);
    }
    private void CreateEffect()
    {
        GameObject shockPrefab  = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_FalseSon.FalseSonMeridiansWillVortexVFX_prefab).WaitForCompletion().InstantiateClone("JellyShockwaveEffect");

        shockPrefab.transform.Find("Distortion").localScale = Vector3.one * PluginConfig.Jelly_Stun_Range.Value / 10;

        ShockwaveEffect = new()
        {
            prefab = shockPrefab,
            prefabName = "JellyShockwaveEffect",
            prefabEffectComponent = shockPrefab.GetComponent<EffectComponent>(),
        };

        ContentAddition.AddEffect(ShockwaveEffect.prefab);
    }

    private void CharacterBody_OnSkillActivated(On.RoR2.CharacterBody.orig_OnSkillActivated orig, CharacterBody self, GenericSkill skill)
    {
        if (UnityEngine.Networking.NetworkServer.active)
        {
            if (self.inventory && self.inventory.GetItemCountEffective(ItemDef) > 0)
            {
                if (self.skillLocator.utility == skill)
                {
                    EffectManager.SimpleEffect(ShockwaveEffect.prefab, self.footPosition, Quaternion.identity, false);
                    Util.PlaySound("Play_GG_Item_DroneShockDamage_Activate", self.gameObject);
                    ShockWave(self);
                }
            }
        }

        orig(self, skill);
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        int itemCount = sender.inventory ? sender.inventory.GetItemCountEffective(ItemDef) : 0;

        if (itemCount > 0) args.cooldownReductionAdd += PluginConfig.Jelly_Cooldown_Base.Value + PluginConfig.Jelly_Cooldown_Stack.Value * (itemCount - 1);
    }
    private void ShockWave(CharacterBody characterBody)
    {
        HG.ListPool<HurtBox>.RentCollection(out List<HurtBox> hurtBoxList);

        SphereSearch radiusSearch = new()
        {
            radius = PluginConfig.Jelly_Stun_Range.Value,
            origin = characterBody.corePosition,
            mask = LayerIndex.entityPrecise.mask,
            queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
        };

        radiusSearch.RefreshCandidates();
        radiusSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(characterBody.master.teamIndex));
        radiusSearch.FilterCandidatesByDistinctHurtBoxEntities();
        radiusSearch.OrderCandidatesByDistance();
        radiusSearch.GetHurtBoxes(hurtBoxList);
        radiusSearch.ClearCandidates();
        
        foreach (HurtBox hurtBox in hurtBoxList)
        {
            CharacterBody victimBody    = hurtBox.healthComponent ? hurtBox.healthComponent.body : null;
            SetStateOnHurt victimState  = victimBody ? victimBody.GetComponent<SetStateOnHurt>() : null;

            if (victimState) victimState.SetStun(PluginConfig.Jelly_Stun_Duration.Value);
        }
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