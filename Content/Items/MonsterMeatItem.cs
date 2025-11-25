using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChefOvercooked;

public class MonsterMeatItem : ItemBase
{
    protected override string Name => "MonsterMeat";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.CannotCopy, 
        ItemTag.CannotSteal, 
        ItemTag.FoodRelated, 
        ItemTag.WorldUnique,
        ItemTag.AIBlacklist,
        ItemTag.CannotDuplicate,
        ItemTag.BrotherBlacklist,
    ];
    protected override bool IsRemovable => false;

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("monsterMeatModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texMonsterMeatIcon");

    protected override string DisplayName => "Monster Meat";

    protected override void Initialize() => ItemDef = Value;
    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 0.05f * 25f;
        modelParam.maxDistance = 0.25f * 25f;
        modelParam.modelRotation = new Quaternion(0.0115291597f, -0.587752283f, 0.0455321521f, -0.807676435f);
    }
    protected override ItemDisplayRuleDict ItemDisplay()
    {
        PickupModelPrefab.AddComponent<ItemDisplay>().rendererInfos = HelperRender.ItemDisplaySetup(PickupModelPrefab);
        ItemDisplayRuleDict baseDisplay = new();

        baseDisplay.Add("ChefBody", new ItemDisplayRule
        {
            followerPrefab = PickupModelPrefab,
            ruleType = ItemDisplayRuleType.ParentedPrefab,

            childName = "Chest",
            localPos = new Vector3(-0.02528F, 0.06014F, 0.01452F),
            localAngles = new Vector3(299.6425F, 13.15949F, 256.5857F),
            localScale = new Vector3(0.075F, 0.075F, 0.075F)

        });

        return baseDisplay;
    }
}