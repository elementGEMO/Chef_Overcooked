using R2API;
using RoR2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using UnityEngine;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths;
using RoR2.Skills;

namespace ChefOvercooked;
using static HelperFontColor;
using static StringHelper;

public class ChefRework
{
    private static GameObject GlazeImpact;

    private static GameObject MeatBallIgnited;
    private static GameObject MeatBallFrozen;
    public ChefRework()
    {
        //BodyCatalog.availability.CallWhenAvailable(ChangeDescriptions);
        ChangeDescriptions();
        CreateAssets();

        // Roll Reworks
        if (PluginConfig.Do_Roll_Rework.Value)
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;
            On.EntityStates.Chef.RolyPoly.StartRolyPoly += RolyPoly_StartRolyPoly;
            On.EntityStates.Chef.RolyPoly.ExitRolyPoly += RolyPoly_ExitRolyPoly;
        }

        // Glaze Reworks
        if (PluginConfig.Do_Glaze_Rework.Value)
            On.EntityStates.Chef.Glaze.FireGrenade += Glaze_FireGrenade;
    }

    private void Glaze_FireGrenade(On.EntityStates.Chef.Glaze.orig_FireGrenade orig, EntityStates.Chef.Glaze self, string targetMuzzle)
    {
        ChefController chefController = self.GetComponent<ChefController>();

        if (chefController)
        {
            bool secondaryOverridden = chefController.secondarySkillName == chefController.secondaryAltSkillOverrideDef.skillName;
            EntityStates.Chef.Glaze.projectilePrefab = secondaryOverridden ? MeatBallFrozen : MeatBallIgnited;
        }

        orig(self, targetMuzzle);
    }
    private void RolyPoly_StartRolyPoly(On.EntityStates.Chef.RolyPoly.orig_StartRolyPoly orig, EntityStates.Chef.RolyPoly self)
    {
        RolyPolyOilBoost oilComponent = self.characterBody.gameObject.GetComponent<RolyPolyOilBoost>();

        if (self.hasBoost && !oilComponent)
        {
            self.characterBody.gameObject.AddComponent<RolyPolyOilBoost>();
            self.hasBoost = false;
        }

        orig(self);
    }
    private void RolyPoly_ExitRolyPoly(On.EntityStates.Chef.RolyPoly.orig_ExitRolyPoly orig, EntityStates.Chef.RolyPoly self)
    {
        RolyPolyOilBoost oilComponent = self.characterBody.gameObject.GetComponent<RolyPolyOilBoost>();

        if (oilComponent)
        {
            UnityEngine.Object.Destroy(oilComponent);
            self.hasBoost = true;
        }

        orig(self);
    }

    private static void ChangeDescriptions()
    {
        // Roll
        if (PluginConfig.Do_Roll_Rework.Value)
        {
            LanguageAPI.AddOverlay("CHEF_UTILITY_DESCRIPTION", FuseText([
                "Stunning".Style(FontColor.cIsDamage) + ". Charge the rolling pin to " + "speed ".Style(FontColor.cIsUtility) + "forward, dealing " + "300%-600% damage ".Style(FontColor.cIsDamage) + "and inflicting " + "oil ".Style(FontColor.cIsUtility) + "to enemies."
            ]));

            LanguageAPI.AddOverlay("CHEF_UTILITY_BOOST_DESCRIPTION", FuseText([
                "Boost".Style(FontColor.cKeywordName) + "Boosting Roll leaves behind a wave of " + "oil ".Style(FontColor.cIsUtility) + "as you speed forward.".Style(FontColor.cSub)
            ]));

            SkillDef rolyPolySkill = Addressables.LoadAssetAsync<SkillDef>(RoR2_DLC2_Chef.ChefRolyPoly_asset).WaitForCompletion();
            rolyPolySkill.keywordTokens = [
                "KEYWORD_STUNNING",
                "KEYWORD_OIL",
                "KEYWORD_OILPOOL",
                "CHEF_UTILITY_BOOST_DESCRIPTION"
            ];
        }

        // Glaze
        if (PluginConfig.Do_Glaze_Rework.Value)
        {
            LanguageAPI.AddOverlay("CHEF_SPECIAL_DESCRIPTION", FuseText([
                "Fire globs of ignited or frozen " + "oil ".Style(FontColor.cIsUtility) + "in quick succession, dealing " + "7x250% damage".Style(FontColor.cIsDamage) + "."
            ]));
        }
    }

    private static void CreateAssets()
    {
        GlazeImpact = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.ChefImpactOil_prefab).WaitForCompletion();

        MeatBallIgnited = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.ChefGlazeProjectileUtilityIgnited_prefab).WaitForCompletion();
        MeatBallFrozen = Addressables.LoadAssetAsync<GameObject>(RoR2_DLC2_Chef.ChefGlazeProjectileUtilityFrozen_prefab).WaitForCompletion();
    }

    private void GlobalEventManager_ProcessHitEnemy(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(
            x => x.MatchLdloc(out _),
            x => x.MatchLdsfld(typeof(DLC2Content.Buffs), nameof(DLC2Content.Buffs.CookingRolled)),
            x => x.MatchCallvirt(typeof(CharacterBody), nameof(CharacterBody.AddBuff))
        ))
        {
            cursor.MoveAfterLabels();

            cursor.Emit(OpCodes.Ldarg_1);   // Load DamageInfo
            cursor.Emit(OpCodes.Ldarg_2);   // Load GameObject

            cursor.EmitDelegate<Action<DamageInfo, GameObject>>((damageInfo, victim) =>
            {
                if (damageInfo.damageType.damageSource != DamageSource.Utility)
                    return;

                CharacterBody victimbody = victim.GetComponent<CharacterBody>();
                CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;

                if (!victimbody || !attackerBody)
                    return;

                victimbody.AddTimedBuff(DLC2Content.Buffs.Oiled, 10f);

                GameObject impact = UnityEngine.Object.Instantiate(GlazeImpact, victimbody.corePosition, Quaternion.identity);
                impact.GetComponent<ProjectileController>().Networkowner = attackerBody.gameObject;
                impact.GetComponent<TeamFilter>().teamIndex = attackerBody.master.teamIndex;
            });
        }
        else Log.Error("CHEFREWORK_PROCESSHITENEMY failed to ILHook");
    }
    public class RolyPolyOilBoost : MonoBehaviour
    {
        CharacterMaster chefMaster;
        Vector3? lastOilPos;
        public void OnEnable()
        {
            CharacterBody chefBody = gameObject.GetComponent<CharacterBody>();
            chefMaster = chefBody ? chefBody.master : null;

            if (!chefMaster)
                return;
        }
        public void FixedUpdate()
        {
            if (!chefMaster)
                return;

            float distance = lastOilPos.HasValue ? Vector3.Distance(lastOilPos.Value, gameObject.transform.position) : 999;

            if (distance > 3)
                SpawnOil();
        }
        private void SpawnOil()
        {
            lastOilPos = gameObject.transform.position;

            Vector3 randomOffset = new(UnityEngine.Random.RandomRange(-4, 4), UnityEngine.Random.RandomRange(-2, 2), UnityEngine.Random.RandomRange(-4, 4));

            GameObject impact = Instantiate(GlazeImpact, gameObject.transform.position + randomOffset, Quaternion.identity);
            impact.GetComponent<ProjectileController>().Networkowner = gameObject;
            impact.GetComponent<TeamFilter>().teamIndex = chefMaster.teamIndex;
            Destroy(impact.GetComponent<AkEvent>());
            Destroy(impact.GetComponent<AkGameObj>());
        }
    }
}