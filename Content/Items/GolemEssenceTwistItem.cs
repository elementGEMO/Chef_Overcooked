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

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class GolemEssenceTwistItem : ItemBase
{
    protected override string Name => "GolemEssenceTwist";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.Healing,
        ItemTag.Utility,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("golemEssenceTwistModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texGolemEssenceTwistIcon");
    protected override string PickupText => "Pickups grant temporary barrier. Gain armor. For igniting the soul.";
    protected override string Description => FuseText([
        string.Format("Picking up " + "an item or other collectible ".Style(FontColor.cIsDamage) + "grants a " + "temporary barrier ".Style(FontColor.cIsHealing) + "for " + "{0}% ".Style(FontColor.cIsHealing) + "of " + "maximum health".Style(FontColor.cIsHealing),
            RoundVal(PluginConfig.Golem_Percent_Barrier.Value)),
        
        string.Format(" or " + "{0} health".Style(FontColor.cIsHealing) + ". ",
            RoundVal(PluginConfig.Golem_Flat_Barrier.Value)),

        string.Format("Increases " + "armor ".Style(FontColor.cIsUtility) + "by " + "{0}".Style(FontColor.cIsUtility) + " ({1} per stack)".Style(FontColor.cStack).OptText(PluginConfig.Golem_Stack_Armor.Value > 0) + ".",
            RoundVal(PluginConfig.Golem_Base_Armor.Value), RoundVal(PluginConfig.Golem_Stack_Armor.Value).SignVal())
    ]);
    protected override string DisplayName => "Golem Essence with a Twist";
    protected override void Initialize()
    {
        ItemDef = Value;

        On.RoR2.CharacterBody.OnPickup += CharacterBody_OnPickup;
        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

        RecipeCatalogChef.AddRecipe("BarrierOnCooldown", "Knurl", Name);
        RecipeCatalogChef.AddRecipe("BarrierOnKill", "Knurl", Name);
    }

    private void CharacterBody_OnPickup(On.RoR2.CharacterBody.orig_OnPickup orig, CharacterBody self, CharacterBody.PickupClass pickupClass)
    {
        orig(self, pickupClass);

        Inventory inventory     = self.inventory;
        HealthComponent health  = self.healthComponent;

        if (!inventory || !health) return;
        if (inventory.GetItemCountEffective(ItemDef) <= 0) return;

        switch (pickupClass)
        {
            case CharacterBody.PickupClass.Item:
            case CharacterBody.PickupClass.TempItem:
                health.AddBarrier(self.maxHealth * PluginConfig.Golem_Percent_Barrier.Value / 100);
                break;
            default:
                health.AddBarrier(PluginConfig.Golem_Flat_Barrier.Value);
                break;
        }
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        args.armorAdd += sender.inventory ? PluginConfig.Golem_Base_Armor.Value + PluginConfig.Golem_Stack_Armor.Value * (sender.inventory.GetItemCount(ItemDef) - 1) : 0;
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