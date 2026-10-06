using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Items;
using RoR2.Projectile;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

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

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("bloodCandiedEyeModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texBloodCandiedEyeIcon");
    protected override string PickupText => "Nearby enemies bleed, and projectiled are slowed. Sweet and crunchy.";
    protected override string Description => FuseText([
        string.Format("Within " + "{0}m".Style(FontColor.cIsUtility) + " ({1}m per stack)".Style(FontColor.cStack).OptText(PluginConfig.Eye_Range_Stack.Value > 0) + ", ",
            RoundVal(PluginConfig.Eye_Range_Base.Value), RoundVal(PluginConfig.Eye_Range_Stack.Value).SignVal()),

        string.Format("enemies start " + "bleeding ".Style(FontColor.cIsDamage) + "for " + "{0}% ".Style(FontColor.cIsDamage) + "base damage ",
            RoundVal(PluginConfig.Eye_Bleed_Damage.Value)),

        string.Format("and projectiles are " + "slowed down ".Style(FontColor.cIsUtility) + "by " + "{0}%".Style(FontColor.cIsUtility) + ".",
            RoundVal(PluginConfig.Eye_Slow_Coefficient.Value))
    ]);
    protected override string DisplayName => "Blood Candied Eye";

    public static GameObject ProjectilePrefab;

    protected override void Initialize()
    {
        ItemDef = Value;

        RecipeCatalogChef.AddRecipe("SlowOnHit", PrimitiveClawsItem.ItemDef.name, Name);
        RecipeCatalogChef.AddRecipe("SlowOnHit", "TeleportOnLowHealth", Name);

        CreateEffect();
    }

    private static void CreateEffect()
    {
        ProjectilePrefab                = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_Railgunner.RailgunnerMineAltDetonated_prefab).WaitForCompletion().InstantiateClone("SlowEyeEffect");
        Transform areaIndicator         = ProjectilePrefab.transform.Find("AreaIndicator");
        MeshRenderer sphere             = areaIndicator.Find("Sphere").GetComponent<MeshRenderer>();
        ParticleSystemRenderer particle = areaIndicator.Find("ChargeIn").GetComponent<ParticleSystemRenderer>();

        ProjectilePrefab.GetComponent<SlowDownProjectiles>().slowDownCoefficient = 0.15f;
        sphere.GetComponent<Rewired.ComponentControls.Effects.RotateAroundAxis>().slowRotationSpeed = 10f;
        sphere.GetComponent<Rewired.ComponentControls.Effects.RotateAroundAxis>().fastRotationSpeed = 10f;

        UnityEngine.Object.Destroy(ProjectilePrefab.GetComponent<ProjectileNetworkTransform>());
        UnityEngine.Object.Destroy(ProjectilePrefab.GetComponent<BuffWard>());

        UnityEngine.Object.Destroy(areaIndicator.Find("Point Light").gameObject);
        UnityEngine.Object.Destroy(areaIndicator.Find("SoftGlow").gameObject);
        UnityEngine.Object.Destroy(areaIndicator.Find("Core").gameObject);

        foreach(Material mat in sphere.materials)
        {
            if (mat.name != "matRailgunnerMineDistortionAlt (Instance)")
                sphere.materials = [mat];
        }

        Material newSphereMat   = new(sphere.sharedMaterial);
        Material newParticleMat = new(particle.sharedMaterial);

        newSphereMat.SetColor("_TintColor", new Color(1, 0.404f, 0.404f, 0.765f));

        newSphereMat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2_DLC3_computationalexchange.texCECloudMask_C_tga).WaitForCompletion());
        newSphereMat.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_TiledTextures.texCloudLightning1_png).WaitForCompletion());
        newSphereMat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_ColorRamps.texRampArchWisp_png).WaitForCompletion());

        newSphereMat.SetFloat("_InvFade", 0.5f);
        newSphereMat.SetFloat("_SoftPower", 1f);
        newSphereMat.SetFloat("_RimPower", 0.25f);
        newSphereMat.SetFloat("_RimStrength", 0.05f);
        newSphereMat.SetFloat("_AlphaBoost", 0.55f);
        newSphereMat.SetFloat("_IntersectionStrength", 17.5f);

        newSphereMat.SetTextureScale("_Cloud1Tex", new Vector2(1, 4));
        newSphereMat.SetTextureScale("_Cloud2Tex", new Vector2(1, 8));

        newParticleMat.SetColor("_TintColor", new Color(1, 0.404f, 0.404f, 0.765f));

        newParticleMat.SetTexture("_MainTex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_ArtifactCompounds.texArtifactCompoundSquareMask_png).WaitForCompletion());
        newParticleMat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_ColorRamps.texRampArchWisp_png).WaitForCompletion());

        sphere.sharedMaterial = newSphereMat;
        particle.sharedMaterial = newParticleMat;
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
    [ItemDefAssociation(useOnServer = true, useOnClient = true)]
    public static ItemDef GetItemDef() => BloodCandiedEyeItem.ItemDef;

    private GameObject slowField;
    private Transform areaVisual;
    private SphereCollider collision;
    private CharacterMaster characterMaster;

    private float currentVelocity;

    public void OnEnable()
    {
        slowField       = Instantiate(BloodCandiedEyeItem.ProjectilePrefab, body.transform);
        areaVisual      = slowField.transform.Find("AreaIndicator");
        collision       = slowField.GetComponent<SphereCollider>();
        characterMaster = body.master;
        currentVelocity = 0f;

        collision.radius = 0f;
    }
    public void FixedUpdate()
    {
        if (!characterMaster && !body.inventory) return;

        float itemRadius = PluginConfig.Eye_Range_Base.Value + PluginConfig.Eye_Range_Stack.Value * (body.inventory.GetItemCountEffective(BloodCandiedEyeItem.ItemDef) - 1);
        float smoothScale = Mathf.SmoothDamp(collision.radius, itemRadius, ref currentVelocity, 0.5f);

        UpdateVisual(smoothScale, Vector3.one);

        if (NetworkServer.active)
        {
            HG.ListPool<HurtBox>.RentCollection(out List<HurtBox> hurtBoxList);

            SphereSearch radiusSearch = new()
            {
                radius = smoothScale,
                origin = body.corePosition,
                mask = LayerIndex.entityPrecise.mask,
                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
            };

            radiusSearch.RefreshCandidates();
            radiusSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(characterMaster.teamIndex));
            radiusSearch.FilterCandidatesByDistinctHurtBoxEntities();
            radiusSearch.OrderCandidatesByDistance();
            radiusSearch.GetHurtBoxes(hurtBoxList);
            radiusSearch.ClearCandidates();

            foreach (HurtBox hurtBox in hurtBoxList)
            {
                CharacterBody victimBody = hurtBox.healthComponent ? hurtBox.healthComponent.body : null;

                if (victimBody != null)
                    DotController.InflictDot(victimBody.gameObject, body.gameObject, hurtBox, DotController.DotIndex.Bleed, 1f, PluginConfig.Eye_Bleed_Damage.Value / 100f * 1f / 2.4f, 1);
            }
        }
    }
    public void UpdateVisual(float radius, Vector3 position, bool updatePos = false)
    {
        collision.radius = radius;
        areaVisual.localScale = Vector3.one * radius;

        if (updatePos)
            areaVisual.position = position;
    }
    public void OnDisable()
    {
        Destroy(slowField);
    }
}