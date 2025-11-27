using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
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
    protected override string PickupText => "Seemingly edible.";
    protected override string Description => "Cooked into " + "temporary food items ".Style(FontColor.cIsUtility) + "by CHEF. Seemingly edible.";
    protected override string Lore => FuseText([
        "Sir, have you ever thought to wonder if these “meals” are even safe to consume?",
        "\n\n",
        "I'd hope so. It's the only food that we have left, and it seems good.. Plus, it's made by CHEF. Seems to have an infinite supply of materials wherever, whenever. Convenient when we need to stop and rest.",
        "\n\n...\n\n",
        "At least it tastes like chicken, most of the time."
    ]);

    protected override void Initialize() => ItemDef = Value;
    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 2f;
        modelParam.maxDistance = 7.5f;
        modelParam.modelRotation = new Quaternion(0.9923118f, 0.0551284f, -0.1102569f, 0.0110257f);
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