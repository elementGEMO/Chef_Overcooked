using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Items;
using RoR2.Projectile;
using RoR2BepInExPack.GameAssetPathsBetter;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class CarbonatedStormItem : ItemBase
{
    protected override string Name => "CarbonatedStorm";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.FoodTier;
    protected override ItemTag[] Tags => [
        ItemTag.Utility,
        ItemTag.Healing,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("carbonatedStormModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texCarbonatedStormIcon");
    protected override string PickupText => "Chance on hit with your Primary skill to drop a bottle cap. Cool and refreshing.";
    protected override string Description => FuseText([
        string.Format("{0}% ".Style(FontColor.cIsUtility) + "chance when hitting your " + "Primary skill ".Style(FontColor.cIsUtility) + "drops a bottle cap that " + "resets your Primary skill cooldown ".Style(FontColor.cIsUtility),
            RoundVal(PluginConfig.Carbonated_Proc_Chance.Value)),
        
        string.Format("and " + "heals ".Style(FontColor.cIsHealing) + "for " + "{0} ".Style(FontColor.cIsHealing) + "({1} per stack) ".Style(FontColor.cStack).OptText(PluginConfig.Carbonated_Heal_Stack.Value > 0) + "health".Style(FontColor.cIsHealing) + ".",
            RoundVal(PluginConfig.Carbonated_Base_Heal.Value), RoundVal(PluginConfig.Carbonated_Heal_Stack.Value).SignVal())
    ]);
    protected override string DisplayName => "Carbonated Storm";

    private static readonly GameObject BottleCapPrefab = ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("bottleCapPickupModel");
    private static GameObject RefreshPack;

    protected override void Initialize()
    {
        ItemDef = Value;

        CreateEffect();

        On.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;

        RecipeCatalogChef.AddRecipe("Bandolier", "SprintBonus", Name);
        RecipeCatalogChef.AddRecipe("Bandolier", "BarrierOnCooldown", Name);
        RecipeCatalogChef.AddRecipe("Bandolier", "HealingPotion", Name);
        RecipeCatalogChef.AddRecipe("Bandolier", "HealingPotionConsumed", Name);
        RecipeCatalogChef.AddRecipe("Bandolier", "AttackSpeedAndMoveSpeed", Name);
    }

    private void CreateEffect()
    {
        RefreshPack         = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Bandolier.AmmoPack_prefab).WaitForCompletion().InstantiateClone("BottleCap");
        GameObject effect   = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Bandolier.AmmoPackPickupEffect_prefab).WaitForCompletion().InstantiateClone("CapEffectPickup");
        GameObject pickup   = RefreshPack.transform.Find("PickupTrigger").gameObject;

        EffectComponent effectComponent = effect.GetComponent<EffectComponent>();
        effectComponent.soundName = "Play_ui_lunar_coin_drop";

        CarbonatedStormPack.PickupEffect = new()
        {
            prefab = effect,
            prefabName = "CapEffectPickup",
            prefabEffectComponent = effect.GetComponent<EffectComponent>()
        };

        pickup.AddComponent<CarbonatedStormPack>();

        Transform visualModel   = RefreshPack.transform.Find("Visuals");
        GameObject bottleCap    = Object.Instantiate(BottleCapPrefab);

        bottleCap.transform.SetParent(visualModel, false);
        bottleCap.transform.Find("modelTransform").localScale = UnityEngine.Vector3.one * 1.5f;
        visualModel.transform.Find("Particle System").localPosition = UnityEngine.Vector3.zero;

        Material capOutline     = new(Addressables.LoadAssetAsync<Material>(RoR2_Base_Bandolier.matPickups_mat).WaitForCompletion());
        MeshRenderer capRender  = bottleCap.transform.Find("modelTransform").Find("mdlCap").gameObject.GetComponent<MeshRenderer>();

        capOutline.SetFloat("_OffsetAmount", 0.0004f);
        capRender.sharedMaterials = [capRender.sharedMaterial, capOutline];

        Object.Destroy(effect.transform.Find("Shells").gameObject);
        Object.Destroy(visualModel.Find("mdlBandolierShell").gameObject);
        Object.Destroy(pickup.GetComponent<AmmoPickup>());

        ContentAddition.AddEffect(CarbonatedStormPack.PickupEffect.prefab);
    }
    private void GlobalEventManager_ProcessHitEnemy(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);

        CharacterBody victimBody    = victim ? victim.GetComponent<CharacterBody>() : null;
        CharacterBody attackerBody  = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
        Inventory inventory         = attackerBody ? attackerBody.inventory : null;
        int itemCount               = inventory ? inventory.GetItemCountEffective(ItemDef) : 0;

        if (itemCount <= 0 || !victimBody || damageInfo.damageType.damageSource != DamageSource.Primary)
            return;

        if (damageInfo.procChainMask.HasProc(ProcType.SureProc) || Util.CheckRoll(PluginConfig.Carbonated_Proc_Chance.Value * damageInfo.procCoefficient, attackerBody.master))
        {
            GameObject refreshPack          = Object.Instantiate(RefreshPack, victim.transform.position, Random.rotation);
            CarbonatedStormPack component   = refreshPack.transform.Find("PickupTrigger").gameObject.GetComponent<CarbonatedStormPack>();
            TeamFilter filter               = refreshPack.GetComponent<TeamFilter>();

            if (component)
                component.itemCount = itemCount;

            if (filter)
                filter.teamIndex = attackerBody.master.teamIndex;

            NetworkServer.Spawn(refreshPack);
        }
    }

    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 6f;
        modelParam.maxDistance = 20f;
    }
}

public class CarbonatedStormPack : MonoBehaviour
{
    public static EffectDef PickupEffect;
    public GameObject baseObject;
    public TeamFilter teamFilter;
    private bool alive = true;
    public int itemCount = 1;

    public void Awake()
    {
        baseObject = gameObject.transform.parent.gameObject;
        teamFilter = baseObject.GetComponent<TeamFilter>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (NetworkServer.active && alive == true && TeamComponent.GetObjectTeam(other.gameObject) == teamFilter.teamIndex)
        {
            SkillLocator skillLocator = other.GetComponent<SkillLocator>();

            if (!skillLocator)
                return;

            CharacterBody characterBody     = other.GetComponent<CharacterBody>();
            HealthComponent healthComponent = characterBody.GetComponent<HealthComponent>();

            if (characterBody)
                characterBody.OnPickup(CharacterBody.PickupClass.Minor);

            if (healthComponent)
                healthComponent.Heal(PluginConfig.Carbonated_Base_Heal.Value + PluginConfig.Carbonated_Heal_Stack.Value * (itemCount - 1), new ProcChainMask());

            alive = false;

            GenericSkill primarySkill = skillLocator.primary;

            if (primarySkill.rechargeStock == 0) primarySkill.Reset();
            else primarySkill.ApplyAmmoPack();

            EffectManager.SimpleEffect(PickupEffect.prefab, transform.position, UnityEngine.Quaternion.identity, true);
            GlobalEventManager.OnCharacterPickupItem(characterBody, PickupIndex.none, 1);
            Destroy(baseObject);
        }
    }
}