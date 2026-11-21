using System;
using R2API;
using RoR2;
using RoR2.UI;
using RoR2.Orbs;
using RoR2.Skills;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPathsBetter;

namespace ChefOvercooked;

public class CookDamageType
{
    public static DamageAPI.ModdedDamageType DamageType;

    public CookDamageType()
    {
        DamageType = DamageAPI.ReserveDamageType();

        IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;

        ExecuteAPI.CalculateAdditiveExecuteThresholdForViewer += CookExecuteVisual;

        MeatOrb.CreatePrefab();
    }
    private void HealthComponent_TakeDamageProcess(ILContext il)
    {
        ILCursor cursor = new(il);
        int executeIndex = -1;

        if (cursor.TryGotoNext(
            x => x.MatchLdcR4(out _),
            x => x.MatchStloc(out executeIndex),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
            x => x.MatchLdfld(typeof(CharacterBody), nameof(CharacterBody.bodyFlags)),
            x => x.MatchLdcI4(out _)
        ) && executeIndex != -1)
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(HealthComponent), "get_isInFrozenState"),
                x => x.MatchBrfalse(out _)
            ))
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldloc, executeIndex);

                cursor.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((self, damageInfo, damagePercent) =>
                {
                    if (DamageAPI.HasModdedDamageType(damageInfo, DamageType) && damagePercent <= PluginConfig.Execute_Threshold.Value) damagePercent = PluginConfig.Execute_Threshold.Value;
                    return damagePercent;
                });

                cursor.Emit(OpCodes.Stloc, executeIndex);
            }
            else Log.Error("COOK_DAMAGE_TYPE_TAKEDAMAGEPROCESS failed to ILHook #2");
        }
        else Log.Error("COOKDAMAGETYPE_TAKEDAMAGEPROCESS failed to ILHook #1");
    }
    private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
    {
        CharacterBody attackerBody  = report.attackerBody;
        CharacterBody victimBody    = report.victimBody;
        DamageInfo damageInfo       = report.damageInfo;

        if (attackerBody != null && victimBody != null && damageInfo != null)
        {
            bool hasDamage      = DamageAPI.HasModdedDamageType(report.damageInfo, DamageType);
            bool hasComponent   = victimBody.GetComponent<MeatTolerance>(); // Checks for leniency for execute

            if (hasDamage || hasComponent)
            {
                attackerBody = hasComponent ? victimBody.GetComponent<MeatTolerance>().GetAttacker() : attackerBody;

                MeatOrb itemOrb = new()
                {
                    stack = 1,
                    origin = victimBody.corePosition,
                    target = attackerBody.mainHurtBox,
                    itemIndex = MonsterMeatItem.ItemDef.itemIndex,
                    inventoryToGrantTo = attackerBody.inventory
                };

                OrbManager.instance.AddOrb(itemOrb);
            }
        }
    }
    private void GlobalEventManager_onServerDamageDealt(DamageReport report)
    {
        CharacterBody attackerBody  = report.attackerBody;
        CharacterBody victimBody    = report.victimBody;
        DamageInfo damageInfo       = report.damageInfo;

        if (attackerBody != null && victimBody != null && damageInfo != null)
        {
            if (DamageAPI.HasModdedDamageType(report.damageInfo, DamageType))
            {
                MeatTolerance meatComponent = victimBody.GetComponent<MeatTolerance>() ?? victimBody.gameObject.AddComponent<MeatTolerance>();
                meatComponent?.SetAttacker(attackerBody);
            }
        }
    }
    private void CookExecuteVisual(CharacterBody victimBody, CharacterBody viewerBody, ref float executeFractionAdd)
    {
        if (victimBody == null || viewerBody == null)
            return;

        if (!viewerBody.GetComponent<ChefController>())
            return;

        GenericSkill genericSpecial = viewerBody.skillLocator ? viewerBody.skillLocator.special : null;
        SkillDef specialSkill       = genericSpecial ? genericSpecial.skillDef : null;

        if (specialSkill != SpecialCookSkill.SkillDef && specialSkill != SpecialScepterCookSkill.SkillDef)
            return;

        executeFractionAdd = Mathf.Clamp(executeFractionAdd, PluginConfig.Execute_Threshold.Value, 1f);
    }

    public class MeatTolerance : MonoBehaviour
    {
        private CharacterBody LastAttacker;
        private float Duration;
        public CharacterBody GetAttacker() => LastAttacker;
        public void SetAttacker(CharacterBody attacker)
        {
            LastAttacker = attacker;
            Duration = PluginConfig.Execute_Leniency.Value;
        }
        public void Awake() => Duration = PluginConfig.Execute_Leniency.Value;
        public void FixedUpdate()
        {
            Duration -= Time.fixedDeltaTime;
            if (Duration < 0f) Destroy(this);
        }
    }
    public class MeatOrb : ItemTransferOrb
    {
        private static GameObject MeatEffect;
        private CharacterBody chefBody;
        public float speed;
        public static void CreatePrefab()
        {
            MeatEffect = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Common_VFX.ItemTransferOrbEffect_prefab).WaitForCompletion().InstantiateClone("MeatOrbEffect");

            GameObject meatCube     = UnityEngine.Object.Instantiate(ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("monsterMeatModel"), MeatEffect.transform);
            GameObject actualMesh   = meatCube.transform.Find("mdlMeat").gameObject;

            actualMesh.transform.localScale *= 0.3f;
            actualMesh.AddComponent<MeatSpin>();

            MeatEffect.transform.Find("BillboardBase").gameObject.SetActive(false);
            MeatEffect.transform.Find("Trail Parent").gameObject.SetActive(false);

            ContentAddition.AddEffect(MeatEffect);
        }

        public override void Begin()
        {
            chefBody = target.healthComponent ? target.healthComponent.body : null;
            travelDuration = UnityEngine.Random.Range(1f, 1.5f) / Mathf.Pow(chefBody.attackSpeed, PluginConfig.Attack_Speed_Effectiveness.Value / 100);

            GameObject previousPrefab = orbEffectPrefab;
            orbEffectPrefab = MeatEffect;

            base.Begin();

            orbEffectPrefab = previousPrefab;
        }
        public override void OnArrival()
        {
            base.OnArrival();

            if (chefBody)
            {
                EffectManager.SimpleSoundEffect(AllSounds.Play_Meat_Pickup.index, chefBody.corePosition, true);
                UserProfile userProfile = chefBody.master?.playerCharacterMasterController?.networkUser?.localUser?.userProfile;
                userProfile?.DiscoverPickup(PickupCatalog.FindPickupIndex(MonsterMeatItem.ItemDef.itemIndex));

                chefBody.AddTimedBuff(MeatTimerBuff.BuffDef, 1.75f / Mathf.Pow(chefBody.attackSpeed, PluginConfig.Attack_Speed_Effectiveness.Value / 100));
            }
        }
    }
    public class MeatSpin : MonoBehaviour
    {
        private Vector3 randomAxis;
        private float randomSpeed;

        public void Awake()
        {
            randomAxis = UnityEngine.Random.onUnitSphere;
            randomSpeed = UnityEngine.Random.Range(200f, 360f);
        }

        public void FixedUpdate()
        {
            transform.Rotate(randomAxis * randomSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }
}