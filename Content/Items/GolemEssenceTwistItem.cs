using EntityStates;
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

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("grilledLizardKebabModel");
    //protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texGrilledLizardKebabIcon");
    protected override string PickupText => "...";
    protected override string Description => FuseText([
        ""
    ]);
    protected override string DisplayName => "Golem Essence with a Twist";
    protected override void Initialize()
    {
        ItemDef = Value;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        //On.RoR2.GenericPickupController.AttemptGrant
        On.RoR2.CharacterBody.OnPickup += CharacterBody_OnPickup;

        //RecipeCatalogChef.AddRecipe(PrimitiveClawsItem.ItemDef.name, "GhostOnKill", Name);
    }

    private void CharacterBody_OnPickup(On.RoR2.CharacterBody.orig_OnPickup orig, CharacterBody self, CharacterBody.PickupClass pickupClass)
    {
        orig(self, pickupClass);

        if (self.inventory && self.inventory.GetItemCountEffective(ItemDef) > 0)
        {
            /*
            switch (pickupClass)
            {
                case CharacterBody.PickupClass.TempItem:
                case CharacterBody.PickupClass.Item:
                    break;
                default:
                    return;
            }
            */

            if (self.healthComponent) self.healthComponent.AddBarrier(self.maxHealth * 0.1f);
        }
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        args.armorAdd += sender.inventory ? sender.inventory.GetItemCount(ItemDef) * 40 : 0;
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

/*
public class GrilledLizardKebabBehavior : BaseItemBodyBehavior
{
    [ItemDefAssociation(useOnServer = true, useOnClient = false)]
    public static ItemDef GetItemDef() => GrilledLizardKebabItem.ItemDef;
    public List<CharacterMaster> spectralLemurians;

    public void OnEnable() => spectralLemurians = [];
    public void FixedUpdate()
    {
        float currentDamage = CalculateFlatDamageMod();
        foreach (CharacterMaster lemMaster in spectralLemurians)
        {
            CharacterBody lemBody = lemMaster.GetBody();
            if (!lemBody) continue;

            lemBody.SetBuffCount(SpectralBuff.BuffDef.buffIndex, Math.Max(1, (int)currentDamage + 1));
        }

        if (spectralLemurians.Count >= PluginConfig.Kebab_Lem_Cap.Value) return;

        MasterSummon masterSummon = new()
        {
            masterPrefab = GrilledLizardKebabItem.devotedLemMaster,
            position = body.transform.position + Vector3.up * 5f,
            rotation = body.transform.rotation,
            summonerBodyObject = body.gameObject,
            useAmbientLevel = true
        };

        CharacterMaster characterMaster = masterSummon.Perform();
        if (characterMaster)
        {
            CharacterBody lemBody = characterMaster.GetBody();
            lemBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            lemBody.AddBuff(RoR2Content.Buffs.Intangible);
            lemBody.AddBuff(SpectralBuff.BuffDef);
            lemBody.transform.localScale *= 1.65f;

            if (characterMaster.inventory)
            {
                characterMaster.inventory.GiveItemPermanent(RoR2Content.Items.Syringe);
                characterMaster.inventory.GiveItemPermanent(RoR2Content.Items.Hoof);
            }

            spectralLemurians.Add(characterMaster);
        }
    }
    private float CalculateFlatDamageMod() => body.damage - body.baseDamage - (body.levelDamage * (body.level - 1));
    public void OnDisable()
    {
        foreach (CharacterMaster lem in spectralLemurians) lem.TrueKill();
        spectralLemurians.Clear();
    }
}
*/