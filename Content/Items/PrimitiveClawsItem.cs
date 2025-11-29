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
    protected override string Description => FuseText([
        "Gain 5% bleed chance. Inflicting bleed increases damage by 15%. Maximum cap of 45% (+30% per stack) damage."
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

        if (hasItem) args.bleedChanceAdd += 5;
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