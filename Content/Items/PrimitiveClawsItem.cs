using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class PrimitiveClawsItem : ItemBase
{
    protected override string Name => "PrimitiveClaws";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.Tier2;
    protected override ItemTag[] Tags => [
        ItemTag.AllowedForUseAsCraftingIngredient,
        ItemTag.Damage,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("primitiveClawsModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texPrimitiveClawsIcon");
    protected override string PickupText => string.Format("Inflicting bleed increases damage. Stacks {0} times.", PluginConfig.Claw_Stack_Cap.Value);
    protected override string Description => FuseText([
        string.Format("Gain " + "{0}% bleed chance".Style(FontColor.cIsDamage) + ". ",
            RoundVal(PluginConfig.Claw_Base_Bleed.Value)),

        string.Format("Inflicting " + "bleed ".Style(FontColor.cIsDamage) + "increases damage by " + "{0}%".Style(FontColor.cIsDamage) + ". ",
            RoundVal(PluginConfig.Claw_Damage_Stack.Value)),

        string.Format("Maximum cap of " + "{0}% ".Style(FontColor.cIsDamage) + "({1}% per stack) ".Style(FontColor.cStack).OptText(PluginConfig.Claw_Stack_Increase.Value > 0) + "damage.",
            RoundVal(PluginConfig.Claw_Damage_Stack.Value * PluginConfig.Claw_Stack_Cap.Value), RoundVal(PluginConfig.Claw_Damage_Stack.Value * PluginConfig.Claw_Stack_Increase.Value).SignVal())
    ]);

    protected override string DisplayName => "Primitive Claws";

    protected override void Initialize()
    {
        ItemDef = Value;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        bool hasItem = sender.inventory ? sender.inventory.GetItemCountEffective(ItemDef) > 0 : false;

        if (hasItem) args.bleedChanceAdd += PluginConfig.Claw_Base_Bleed.Value;
    }

    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 2f;
        modelParam.maxDistance = 7.5f;
        modelParam.modelRotation = new Quaternion(-0.9999383f, 0, 0, 0.0111104f);
    }
}