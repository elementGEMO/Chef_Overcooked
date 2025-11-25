using System;
using System.Collections.Generic;
using System.Text;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.Orbs;
using RoR2BepInExPack.GameAssetPathsBetter;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChefOvercooked;

public class CookDamageType
{
    public static DamageAPI.ModdedDamageType DamageType;

    public CookDamageType()
    {
        DamageType = DamageAPI.ReserveDamageType();
        IL.RoR2.ItemDef.AttemptGrant += ItemDef_AttemptGrant;
        IL.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
        IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

        MeatOrb.CreatePrefab();
    }

    private void ItemDef_AttemptGrant(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(typeof(PickupDef.GrantContext), nameof(PickupDef.GrantContext.body)),
            x => x.MatchCallvirt(typeof(CharacterBody), "get_inventory")
        ))
        {
            if (cursor.TryGotoNext(x => x.MatchLdcR4(out _)))
            {
                cursor.Remove();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldobj, typeof(PickupDef.GrantContext));
                cursor.EmitDelegate<Func<PickupDef.GrantContext, float>>(context => context.controller.pickup.decayValue);
            }
            else Log.Error("COOK_DAMAGE_TYPE_ATTEMPTGRANT_1 failed to ILHook #2");
        }
        else Log.Error("COOK_DAMAGE_TYPE_ATTEMPTGRANT_1 failed to ILHook #1");

        cursor              = new(il);
        ILLabel breakState  = null;
        int pickupDefIndex  = -1;
        int duplicateIndex = -1;

        cursor.TryGotoNext(
            x => x.MatchLdfld(typeof(PickupDef), nameof(PickupDef.itemIndex)),
            x => x.MatchStloc(out pickupDefIndex)
        );

        cursor.TryGotoNext(
            x => x.MatchLdsfld(typeof(DLC3Content.Items), nameof(DLC3Content.Items.Duplicator)),
            x => x.MatchCallvirt(typeof(Inventory), nameof(Inventory.GetItemCountEffective)),
            x => x.MatchStloc(out duplicateIndex)
        );

        if (pickupDefIndex != -1 && duplicateIndex != -1)
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdloc(out _),
                x => x.MatchLdcI4(0),
                x => x.MatchCgt(),
                x => x.MatchLdloc(out _),
                x => x.MatchAnd(),
                x => x.MatchBrfalse(out breakState)
            ) && breakState != null)
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldobj, typeof(PickupDef.GrantContext));
                cursor.Emit(OpCodes.Ldloc, pickupDefIndex);
                cursor.Emit(OpCodes.Ldloc, duplicateIndex);

                cursor.EmitDelegate<Action<PickupDef.GrantContext, ItemIndex, int>>((context, itemIndex, itemCount) =>
                {
                    bool isDuplicated = !context.controller.Duplicated;
                    CharacterBody pickerBody = context.body;

                    if (itemCount > 0 && isDuplicated)
                    {
                        EffectData effectData = new()
                        {
                            origin = pickerBody.corePosition,
                            start = pickerBody.corePosition,
                            scale = pickerBody.radius
                        };
                        EffectManager.SpawnEffect(CharacterBody.CommonAssets.duplicatorPrefab, effectData, true);
                        pickerBody.inventory.GiveItemTemp(itemIndex, context.controller.pickup.decayValue);
                    }
                });

                cursor.Emit(OpCodes.Br, breakState);
            }
        }
    }
    private void HealthBar_UpdateBarInfos(ILContext il)
    {
        ILCursor cursor = new(il);
        int cullIndex = -1;

        if (cursor.TryGotoNext(
            x => x.MatchLdloc(out _),
            x => x.MatchLdfld(typeof(HealthComponent.HealthBarValues), nameof(HealthComponent.HealthBarValues.cullFraction)),
            x => x.MatchStloc(out cullIndex)
        ) && cullIndex != -1)
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda(typeof(HealthBar), nameof(HealthBar.barInfoCollection)),
                x => x.MatchLdflda(typeof(HealthBar.BarInfoCollection), nameof(HealthBar.BarInfoCollection.cullBarInfo))
            ))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, cullIndex);

                cursor.EmitDelegate<Func<HealthBar, float, float>>((self, cull) =>
                {
                    CharacterBody victimBody = self.source ? self.source.body : null;
                    CharacterBody viewerBody = self.viewerBody;

                    if (victimBody != null && viewerBody != null)
                    {
                        bool executeImmune = (victimBody.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) > CharacterBody.BodyFlags.None;
                        bool isViewerChef = viewerBody.GetComponent<ChefController>();

                        if (!executeImmune && isViewerChef)
                        {
                            SkillDef selectSpecial = viewerBody.skillLocator?.special?.skillDef;
                            bool hasCookAbility = selectSpecial ? selectSpecial == SpecialCookSkill.SkillDef : false;

                            Log.Error(selectSpecial.skillName);
                            Log.Error(SpecialCookSkill.SkillDef.skillName);

                            if (hasCookAbility) cull = Math.Max(cull, 0.1f);
                        }
                    }

                    return cull;
                });

                cursor.Emit(OpCodes.Stloc, cullIndex);
            }
            else Log.Error("COOKDAMAGETYPE_UPDATEBARINFOS failed to ILHook #2");
        }
        else Log.Error("COOKDAMAGETYPE_UPDATEBARINFOS failed to ILHook #1");
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
                    if (DamageAPI.HasModdedDamageType(damageInfo, DamageType) && damagePercent < 0.1f) damagePercent = 0.1f;
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
        CharacterBody attackerBody = report.attackerBody;
        CharacterBody victimBody = report.victimBody;
        DamageInfo damageInfo = report.damageInfo;

        if (attackerBody != null && victimBody != null && damageInfo != null)
        {
            if (DamageAPI.HasModdedDamageType(report.damageInfo, DamageType))
            {
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
            orbEffectPrefab = MeatEffect;
            travelDuration *= UnityEngine.Random.Range(0.5f, 1.5f);
            base.Begin();
        }
        public override void OnArrival()
        {
            base.OnArrival();

            if (chefBody)
            {
                UserProfile userProfile = chefBody.master?.playerCharacterMasterController?.networkUser?.localUser?.userProfile;
                Util.PlaySound("Play_UI_item_pickup", chefBody.gameObject);
                chefBody.AddTimedBuff(MeatTimerBuff.BuffDef, 1f);
                if (userProfile != null) userProfile.DiscoverPickup(PickupCatalog.FindPickupIndex(MonsterMeatItem.ItemDef.itemIndex));
            }
        }
    }
    public class MeatSpin : MonoBehaviour
    {
        private Vector3 randomAxis;
        private float randomSpeed;

        private void Awake()
        {
            randomAxis = UnityEngine.Random.onUnitSphere;
            randomSpeed = UnityEngine.Random.Range(200f, 360f);
        }

        private void Update()
        {
            transform.Rotate(randomAxis * randomSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }
}