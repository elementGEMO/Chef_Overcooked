using System;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2BepInExPack.GameAssetPathsBetter;

namespace ChefOvercooked
{
    public class Cook : GenericCharacterMain
    {
        private static EffectDef SpinEffect;
        private static EffectDef SliceEffect;

        private static readonly int AttackInstances = 6;
        private static readonly float DamageCoefficient = 0.8f;
        private static readonly float BaseAttackRate = 0.1f;
        private static readonly float Radius = 7f;

        private ChefController chefControl;
        private EffectComponent sliceImpact;
        private float setDuration;
        private int attackCount;

        public static void Instantiate()
        {
            //SpinEffect = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.BoostedRolyPolyProjectile_prefab).WaitForCompletion();

            GameObject spinPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.BoostedRolyPolyGhost_prefab).WaitForCompletion().InstantiateClone("CookSpinEffect");
            EffectComponent spinEffect = spinPrefab.AddComponent<EffectComponent>();
            DestroyOnTimer spinTimer = spinPrefab.GetComponent<DestroyOnTimer>();

            spinTimer.enabled = true;
            spinTimer.duration = BaseAttackRate * 5;
            spinEffect.parentToReferencedTransform = true;
            spinEffect.positionAtReferencedTransform = true;
            spinPrefab.AddComponent<VFXAttributes>();
            UnityEngine.Object.Destroy(spinPrefab.GetComponent<ProjectileGhostController>());

            SpinEffect = new()
            {
                prefab = spinPrefab,
                prefabName = "CookSpinEffect",
                prefabEffectComponent = spinEffect
            };

            //

            GameObject slicePrefab      = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Saw.OmniImpactVFXSawmerang_prefab).WaitForCompletion().InstantiateClone("CookSliceImpact");
            EffectComponent sliceEffect = slicePrefab.AddComponent<EffectComponent>();

            UnityEngine.Object.Destroy(slicePrefab.transform.Find("Scaled Hitspark 4, Directional (Random Color)").gameObject);
            UnityEngine.Object.Destroy(slicePrefab.transform.Find("Scaled Hitspark 3, Radial (Random Color)").gameObject);
            UnityEngine.Object.Destroy(slicePrefab.transform.Find("Dash, Bright").gameObject);

            SliceEffect = new()
            {
                prefab = slicePrefab,
                prefabName = "CookSliceImpact",
                prefabEffectComponent = sliceEffect
            };

            //

            ContentAddition.AddEffect(SpinEffect.prefab);
            ContentAddition.AddEffect(SliceEffect.prefab);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            chefControl = characterBody.GetComponent<ChefController>();
            sliceImpact = SliceEffect.prefab.GetComponent<EffectComponent>();
            setDuration = BaseAttackRate;
            attackCount = 0;

            chefControl.blockOtherSkills = true;
            PlayAnimation("Body", "BoostedRolyPoly", "FireRolyPoly.playbackRate", BaseAttackRate * 2, 0f);
            GetModelAnimator().SetBool("isInBoostedRolyPoly", true);

            Util.PlaySound("Play_chef_skill3_boosted_active_loop", gameObject);
        }
        public override void OnExit()
        {
            GetModelAnimator().SetBool("isInBoostedRolyPoly", false);
            PlayCrossfade("Body", "ExitRolyPoly", 0.1f);
            chefControl.blockOtherSkills = false;

            Util.PlaySound("Stop_chef_skill3_boosted_active_loop", gameObject);

            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (chefControl == null) return;

            setDuration -= GetDeltaTime();
            if (setDuration <= 0)
            {
                setDuration = BaseAttackRate;
                attackCount += 1;

                AreaSlash();
            }

            if (attackCount > AttackInstances && isAuthority) outer.SetNextStateToMain();
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
                radius = Radius,
                baseDamage = damageStat * DamageCoefficient,
                damageColorIndex = DamageColorIndex.Default,
                damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.ChefSource, DamageSource.Special),
                crit = Util.CheckRoll(critStat, characterBody.master),
                falloffModel = BlastAttack.FalloffModel.None,
                impactEffect = sliceImpact.effectIndex,
                procCoefficient = 1
            };

            areaSlash.Fire();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}
