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
    private EffectComponent sliceImpact;
    private float setDuration;
    private int attackCount;
    private bool isCrit;
    
    public static void CreateEffects()
    {
        GameObject spinPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.BoostedRolyPolyGhost_prefab).WaitForCompletion().InstantiateClone("CookSpinEffect");
        EffectComponent spinEffect = spinPrefab.AddComponent<EffectComponent>();
        DestroyOnTimer spinTimer = spinPrefab.GetComponent<DestroyOnTimer>();

        spinTimer.enabled = true;
        spinTimer.duration = Math.Max(0.5f, PluginConfig.Attack_Rate.Value / PluginConfig.Attack_Instances.Value);
        spinEffect.parentToReferencedTransform = true;
        spinEffect.positionAtReferencedTransform = true;
        spinPrefab.AddComponent<VFXAttributes>();
        UnityEngine.Object.Destroy(spinPrefab.GetComponent<ProjectileGhostController>());

        Vector3 spinSize    = Vector3.one * 0.75f * PluginConfig.Radius.Value / 2;
        spinSize            = new Vector3(spinSize.x, spinSize.y, Mathf.Sqrt(spinSize.z));

        foreach (Transform scale in spinPrefab.GetComponentInChildren<Transform>()) scale.localScale = spinSize;

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
        sliceImpact = SliceEffect.prefab.GetComponent<EffectComponent>();
        isCrit = Util.CheckRoll(critStat, characterBody.master);

        chefControl.blockOtherSkills = true;
        setDuration = PluginConfig.Attack_Rate.Value / PluginConfig.Attack_Instances.Value;
        attackCount = 0;

        Util.PlaySound("Play_chef_skill3_boosted_active_loop", gameObject);
        PlayAnimation("Body", "BoostedRolyPoly", "FireRolyPoly.playbackRate", PluginConfig.Attack_Rate.Value, 0f);
        GetModelAnimator().SetBool("isInBoostedRolyPoly", true);

        if (NetworkServer.active) characterBody.AddBuff(CookingBuff.BuffDef);
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (chefControl == null) return;

        setDuration -= GetDeltaTime();
        if (setDuration <= 0)
        {
            setDuration = PluginConfig.Attack_Rate.Value / PluginConfig.Attack_Instances.Value;
            attackCount += 1;

            characterBody.AddTimedBuff(MeatTimerBuff.BuffDef, 1.5f);
            AreaSlash();
        }

        if (attackCount > PluginConfig.Attack_Instances.Value && isAuthority)
        {
            outer.SetNextStateToMain();
        }
    }
    public override void OnExit()
    {
        chefControl.blockOtherSkills = false;
        Util.PlaySound("Stop_chef_skill3_boosted_active_loop", gameObject);
        GetModelAnimator().SetBool("isInBoostedRolyPoly", false);
        PlayCrossfade("Body", "ExitRolyPoly", 0.1f);

        if (NetworkServer.active) characterBody.RemoveBuff(CookingBuff.BuffDef);

        base.OnExit();
    }

    private void AreaSlash()
    {
        if (!isAuthority) return;

        EffectManager.SpawnEffect(SpinEffect.prefab, new EffectData()
        {
            rootObject = gameObject,
            origin = characterBody.corePosition,
            rotation = Quaternion.identity
        }, true);

        BlastAttack areaSlash = new()
        {
            inflictor = gameObject,
            attacker = gameObject,
            attackerFiltering = AttackerFiltering.NeverHitSelf,
            position = characterBody.corePosition,
            teamIndex = GetTeam(),
            radius = PluginConfig.Radius.Value,
            procCoefficient = PluginConfig.Proc_Coefficient.Value,
            baseDamage = damageStat * PluginConfig.Damage_Coefficient.Value / 100,
            damageColorIndex = DamageColorIndex.Default,
            damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.ChefSource, DamageSource.Special),
            crit = isCrit,
            falloffModel = BlastAttack.FalloffModel.None,
            impactEffect = sliceImpact.effectIndex
        };

        DamageAPI.AddModdedDamageType(areaSlash, CookDamageType.DamageType);
        areaSlash.Fire();
    }
    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
}
