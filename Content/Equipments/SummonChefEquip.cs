using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;

public class SummonChefEquip : EquipmentBase
{
    protected override string Name => "SummonChefEquip";
    public static EquipmentDef EquipmentDef;
    protected override float Cooldown => 60f;

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("summonChefModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texSummonChefIcon");
    protected override string PickupText => "Call in Wandering CHEF. Consumed on use.";
    protected override string Description => FuseText([
        "Call in Wandering CHEF from the " + "Computational Exchange".Style(FontColor.cIsUtility) + ". ",

        "Arriving in " + "5 seconds".Style(FontColor.cIsUtility) + ", dealing " + "2000% damage ".Style(FontColor.cIsDamage) + "and " + "consuming ".Style(FontColor.cIsUtility) + "this equipment."
    ]);
    protected override string DisplayName => "À La Carte";

    public static GameObject ChefProjectile;

    protected override void Initialize()
    {
        EquipmentDef = Value;
        CreateProjectile();
    }
    private void CreateProjectile()
    {
        ChefProjectile          = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_VendingMachine.VendingMachineProjectile_prefab).WaitForCompletion().InstantiateClone("ChefProjectile");
        GameObject spawnChef    = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC3_MealPrep.MealPrep_prefab).WaitForCompletion().InstantiateClone("MealPrepDeployable");
        GameObject chefMesh     = spawnChef.transform.GetChild(0).gameObject;
        AlignToNormal normal    = spawnChef.AddComponent<AlignToNormal>();

        spawnChef.AddComponent<Deployable>();
        chefMesh.AddComponent<ChefDeployAdjuster>();
        chefMesh.transform.localPosition = Vector3.zero;

        normal.maxDistance = 2;
        normal.offsetDistance = 1;

        GameObject caffeinePrefab   = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC1_VendingMachine.VendingMachine_prefab_e69e4c37).WaitForCompletion().InstantiateClone("Temp");
        Transform terrainOne        = caffeinePrefab.transform.Find("Terrain (1)");

        terrainOne.SetParent(spawnChef.transform);
        terrainOne.transform.localPosition = Vector3.zero;

        Object.Destroy(caffeinePrefab);
        ChefProjectile.GetComponent<ProjectileInstantiateDeployable>().prefab = spawnChef;
    }
    protected override bool ActivateEquip(EquipmentSlot slot)
    {
        CharacterBody characterBody = slot.characterBody;
        Ray ray = new(slot.GetAimRay().origin, Vector3.down);

        if (Util.CharacterRaycast(characterBody.gameObject, ray, out RaycastHit rayInfo, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
        {
            ProjectileManager.instance.FireProjectileWithoutDamageType(ChefProjectile, rayInfo.point, Quaternion.identity, characterBody.gameObject, characterBody.damage, 0f, Util.CheckRoll(characterBody.crit, characterBody.master), DamageColorIndex.Default, null, -1f);
            
            if (characterBody.inventory)
                characterBody.inventory.SetEquipmentIndex(ConsumedChefEquip.EquipmentDef.equipmentIndex, true);

            return true;
        }

        return false;
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

    private class ChefDeployAdjuster : MonoBehaviour
    {
        private static readonly float SPEED_COEFF = 10f;
        private float squashHeightVelocity = 0f;
        private float squashSideVelocity = 0f;
        private float squashTime = 0f;
        public void Awake() => transform.Rotate(0, Random.Range(-360, 360), 0);
        public void FixedUpdate()
        {
            float heightSquash = 1f, sideSquash = 1;
            squashTime += Time.deltaTime;

            if (squashTime <= Mathf.PI / SPEED_COEFF) {
                heightSquash = -0.55f * Mathf.Pow(squashTime * SPEED_COEFF, 3) + 2.5f * Mathf.Pow(squashTime * SPEED_COEFF, 2) - 2.55f * (squashTime * SPEED_COEFF) + 1f;
                sideSquash = 0.5f * Mathf.Pow(squashTime * SPEED_COEFF, 3) - 2.5f * Mathf.Pow(squashTime * SPEED_COEFF, 2) + 3f * (squashTime * SPEED_COEFF) + 1f;
            }

            float smoothHeight = Mathf.SmoothDamp(transform.localScale.y, heightSquash, ref squashHeightVelocity, 0.025f);
            float smoothSide = Mathf.SmoothDamp(transform.localScale.x, sideSquash, ref squashSideVelocity, 0.025f);

            transform.localScale = new Vector3(smoothSide, smoothHeight, smoothSide);

            if (squashTime >= Mathf.PI / SPEED_COEFF * 2)
                enabled = false;
        }
    }
}