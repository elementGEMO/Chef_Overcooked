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
public class GrilledLizardKebabItem : ItemBase
{
    protected override string Name => "GrilledLizardKebab";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.Damage,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("grilledLizardKebabModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texGrilledLizardKebabIcon");
    protected override string PickupText => "Increases base damage and spawns a spectral Lemurian that inherits damage bonuses. It's still sizzling.";
    protected override string Description => FuseText([
        string.Format("Increase base damage by " + "{0}%".Style(FontColor.cIsDamage) + " ({1}% per stack)".Style(FontColor.cStack).OptText(PluginConfig.Kebab_Stack_Damage.Value > 0) + ". ",
            RoundVal(PluginConfig.Kebab_Base_Damage.Value), RoundVal(PluginConfig.Kebab_Stack_Damage.Value).SignVal()),
        
        string.Format("Gain a spectral Lemurian that inherits " + "{0}% ".Style(FontColor.cIsDamage) + "of your " + "damage bonuses".Style(FontColor.cIsDamage) + ".",
            RoundVal(PluginConfig.Kebab_Lem_Damage.Value))
    ]);
    protected override string DisplayName => "Grilled Lizard Kebab";

    public static GameObject devotedLemMaster;
    public static CraftableDef lizardKebabRecipe;
    protected override void Initialize()
    {
        ItemDef = Value;

        devotedLemMaster = CreateLemurianMaster();
        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

        RecipeCatalogChef.AddRecipe(PrimitiveClawsItem.ItemDef.name, "DeathProjectile", Name);
    }
    private GameObject CreateLemurianMaster()
    {
        GameObject masterPrefab = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(RoR2_CU8_LemurianEgg.DevotedLemurianMaster_prefab).WaitForCompletion();
        BaseAI lemurianAI       = masterPrefab ? masterPrefab.GetComponent<BaseAI>() : null;

        if (lemurianAI)
        {
            lemurianAI.copyLeaderTarget = true;
            lemurianAI.aimVectorMaxSpeed = 200f;
            lemurianAI.aimVectorDampTime = 0.05f;
        }

        foreach (AISkillDriver skill in masterPrefab.GetComponents<AISkillDriver>())
        {
            switch (skill.customName)
            {
                case "DevotedSecondarySkill":
                    skill.maxDistance = 5f;
                    break;
                case "StrafeAndShoot":
                    skill.maxDistance = 30;
                    skill.activationRequiresAimTargetLoS = true;
                    break;
                case "StopAndShoot":
                    skill.maxDistance = 65;
                    skill.minDistance = 30;
                    skill.activationRequiresAimTargetLoS = true;
                    skill.aimType = AISkillDriver.AimType.AtMoveTarget;
                    break;
                case "ReturnToLeaderDefault":
                    skill.minDistance = 10;
                    skill.shouldSprint = true;
                    break;
                case "Chase":
                    skill.shouldSprint = true;
                    break;
            }
        }

        UnityEngine.Object.Destroy(masterPrefab.GetComponent<DevotedLemurianController>());

        return masterPrefab;
    }
    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        int itemCount = sender.inventory ? sender.inventory.GetItemCountEffective(ItemDef) : 0;

        if (itemCount > 0) args.damageMultAdd += (PluginConfig.Kebab_Base_Damage.Value + PluginConfig.Kebab_Stack_Damage.Value * (itemCount - 1)) / 100;
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