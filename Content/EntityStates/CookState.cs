using System;
using R2API;
using RoR2;
using RoR2.Projectile;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPathsBetter;

namespace ChefOvercooked;
public class CookState : GenericCharacterMain
{
    private static EffectDef SpinEffect;
    private static EffectDef SliceEffect;

    private ChefController chefControl;
    private float setDuration;
    private float attackCount;
    private bool isCrit;

    private float reducedAttackSpeed;
    private float finalDamageCoeff;
    private float finalRadius;
    private int finalAttackCount;

    private bool isScepter;
    
    public static void CreateEffects()
    {
        GameObject spinPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.BoostedRolyPolyGhost_prefab).WaitForCompletion().InstantiateClone("CookSpinEffect");
        EffectComponent spinEffect = spinPrefab.AddComponent<EffectComponent>();
        DestroyOnTimer spinTimer = spinPrefab.GetComponent<DestroyOnTimer>();

        spinTimer.enabled = true;
        spinTimer.duration = 0.5f;
        spinEffect.parentToReferencedTransform = true;
        spinEffect.positionAtReferencedTransform = true;
        spinEffect.applyScale = true;

        spinPrefab.AddComponent<VFXAttributes>();
        UnityEngine.Object.Destroy(spinPrefab.GetComponent<ProjectileGhostController>());

        foreach (Transform effect in spinPrefab.transform)
        {
            effect.transform.localScale = new Vector3(0.6f, 0.6f, 0.4f);
            effect.GetComponent<ParticleSystem>().scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        SpinEffect = new()
        {
            prefab = spinPrefab,
            prefabName = "CookSpinEffect",
            prefabEffectComponent = spinEffect
        };

        GameObject slicePrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Saw.OmniImpactVFXSawmerang_prefab).WaitForCompletion().InstantiateClone("CookSliceImpact");
        EffectComponent sliceEffect = slicePrefab.GetComponent<EffectComponent>();

        UnityEngine.Object.Destroy(slicePrefab.transform.Find("Scaled Hitspark 4, Directional (Random Color)").gameObject);
        UnityEngine.Object.Destroy(slicePrefab.transform.Find("Scaled Hitspark 3, Radial (Random Color)").gameObject);
        UnityEngine.Object.Destroy(slicePrefab.transform.Find("Dash, Bright").gameObject);

        //foreach (Transform scale in slicePrefab.GetComponentInChildren<Transform>()) scale.localScale *= 5;

        SliceEffect = new()
        {
            prefab = slicePrefab,
            prefabName = "CookSliceImpact",
            prefabEffectComponent = sliceEffect
        };

        ContentAddition.AddEffect(SpinEffect.prefab);
        ContentAddition.AddEffect(SliceEffect.prefab);
    }
    public override void OnEnter()
    {
        base.OnEnter();

        chefControl = characterBody.GetComponent<ChefController>();
        isCrit      = Util.CheckRoll(critStat, characterBody.master);

        reducedAttackSpeed  = Mathf.Pow(attackSpeedStat, PluginConfig.Attack_Speed_Effectiveness.Value / 100);
        finalDamageCoeff    = PluginConfig.Damage_Coefficient.Value / 100f;
        finalRadius         = PluginConfig.Radius.Value;
        finalAttackCount    = PluginConfig.Attack_Instances.Value;

        isScepter = skillLocator.special.skillDef == SpecialScepterCookSkill.SkillDef;
        if (isScepter)
        {
            reducedAttackSpeed /= PluginConfig.Attack_Instances_Scepter.Value / 1.5f;
            finalDamageCoeff *= PluginConfig.Damage_Coefficient_Scepter.Value;
            finalRadius *= Mathf.Pow(attackSpeedStat, PluginConfig.Radius_Effectiveness.Value / 100); ;
            finalAttackCount = Mathf.FloorToInt(finalAttackCount * PluginConfig.Attack_Instances_Scepter.Value);
        }

        chefControl.blockOtherSkills = true;
        attackCount = 0;

        Util.PlaySound("Play_chef_skill3_boosted_active_loop", gameObject);
        PlayAnimation("Body", "BoostedRolyPoly", "FireRolyPoly.playbackRate", PluginConfig.Attack_Rate.Value / attackSpeedStat, 0f);
        GetModelAnimator().SetBool("isInBoostedRolyPoly", true);

        UpdateDuration();

        if (NetworkServer.active)
            characterBody.AddBuff(CookingBuff.BuffDef);
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!chefControl)
            return;

        SlashEffectSpawn();

        if (NetworkServer.active)
            characterBody.AddTimedBuff(MeatTimerBuff.BuffDef, 0.1f / reducedAttackSpeed * (isScepter ? PluginConfig.Attack_Instances_Scepter.Value / 1.5f : 1));

        setDuration -= GetDeltaTime();
        if (setDuration <= 0)
        {
            attackCount += 1;

            UpdateDuration();
            AreaSlash();
        }

        if (attackCount > finalAttackCount && isAuthority)
        {
            if (characterBody.inventory && characterBody.inventory.GetItemCountEffective(MonsterMeatItem.ItemDef) > 0 && NetworkServer.active)
                characterBody.AddTimedBuff(MeatTimerBuff.BuffDef, 2f / reducedAttackSpeed * (isScepter ? PluginConfig.Attack_Instances_Scepter.Value / 1.5f : 1));

            outer.SetNextStateToMain();
        }
    }
    private void UpdateDuration() => setDuration = PluginConfig.Attack_Rate.Value / finalAttackCount / reducedAttackSpeed;
    public override void OnExit()
    {
        chefControl.blockOtherSkills = false;

        Util.PlaySound("Stop_chef_skill3_boosted_active_loop", gameObject);
        GetModelAnimator().SetBool("isInBoostedRolyPoly", false);
        PlayCrossfade("Body", "ExitRolyPoly", 0.1f);

        if (NetworkServer.active)
            characterBody.RemoveBuff(CookingBuff.BuffDef);

        base.OnExit();
    }
    private void SlashEffectSpawn()
    {
        EffectManager.SpawnEffect(SpinEffect.prefab, new EffectData()
        {
            rootObject = gameObject,
            origin = characterBody.corePosition,
            rotation = Quaternion.identity,
            scale = finalRadius / 2
        }, true);
    }
    private void AreaSlash()
    {
        if (!isAuthority) return;

        BlastAttack areaSlash = new()
        {
            inflictor = gameObject,
            attacker = gameObject,
            attackerFiltering = AttackerFiltering.NeverHitSelf,
            position = characterBody.corePosition,
            teamIndex = GetTeam(),
            radius = finalRadius,
            procCoefficient = PluginConfig.Proc_Coefficient.Value,
            baseDamage = damageStat * finalDamageCoeff,
            damageColorIndex = DamageColorIndex.Default,
            damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Special),
            crit = isCrit,
            falloffModel = BlastAttack.FalloffModel.None,
            impactEffect = SliceEffect.index
        };

        DamageAPI.AddModdedDamageType(areaSlash, CookDamageType.DamageType);
        areaSlash.Fire();
    }
    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
}
