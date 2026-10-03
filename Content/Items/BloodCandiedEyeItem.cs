using EntityStates;
using R2API;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Items;
using RoR2.Projectile;
using RoR2BepInExPack.GameAssetPathsBetter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class BloodCandiedEyeItem : ItemBase
{
    protected override string Name => "BloodCandiedEye";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.Damage,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("grilledLizardKebabModel");
    //protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texGrilledLizardKebabIcon");
    protected override string PickupText => "...";
    protected override string Description => FuseText([
        "..."
    ]);
    protected override string DisplayName => "Blood Candied Eye";

    public static GameObject ProjectilePrefab;

    protected override void Initialize()
    {
        ItemDef = Value;

        CreateEffect();

        //RecipeCatalogChef.AddRecipe(PrimitiveClawsItem.ItemDef.name, "GhostOnKill", Name);
    }

    private static void CreateEffect()
    {
        GameObject explodePrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Railgunner.RailgunnerMineAltDetonated_prefab).WaitForCompletion().InstantiateClone("SlowEyeEffect");

        explodePrefab.GetComponent<BuffWard>().expires = false;
        explodePrefab.GetComponent<SlowDownProjectiles>().slowDownCoefficient = 0.25f;

        UnityEngine.Object.Destroy(explodePrefab.GetComponent<ProjectileNetworkTransform>());

        ProjectilePrefab = explodePrefab;
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

public class BloodCandiedEyeBehavior : BaseItemBodyBehavior
{
    [ItemDefAssociation(useOnServer = true, useOnClient = false)]
    public static ItemDef GetItemDef() => BloodCandiedEyeItem.ItemDef;

    private GameObject SlowField;

    public void OnEnable()
    {
        SlowField = Instantiate(BloodCandiedEyeItem.ProjectilePrefab, body.transform);
    }
    public void FixedUpdate()
    {
        //SlowField.GetComponent<UnityEngine.Networking.NetworkTransform>().transform = SlowField.
        //SlowField.transform.position = body.corePosition;

        // https://github.com/elementGEMO/ScalesOfTheAsclepius/blob/master/Content/SharedHooks/Items/IVBagHooks.cs
        // Return to IVBagHooks for a reminder on "INetMessages" w/ "R2API Networking"
    }
    public void OnDisable()
    {
        Destroy(SlowField);
    }
}