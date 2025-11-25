using BepInEx;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2BepInExPack.GameAssetPathsBetter;
using ShaderSwapper;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ChefOvercooked
{
    [BepInDependency(ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ChefOverCookedPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginCreator + "." + PluginName;
        public const string PluginCreator = "noodleGemo";
        public const string PluginName = "Chef_Overcooked";
        public const string PluginVersion = "1.0.0";

        public static ChefOverCookedPlugin Instance { get; private set; }
        public static AssetBundle Bundle { get; private set; }

        public static readonly string TokenPrefix = "GEMO_CHEF_OVERCOOKED_";

        public void Awake()
        {
            Instance = this;
            SetUpAssets();

            PluginConfig.Init();
            if (PluginConfig.Enable_Logging.Value) Log.Init(Logger);

            Cook.Instantiate();
            new MonsterMeatItem();
            new MeatTimerBuff();

            new CookDamageType();
            new CookingBuff();
            new CookedBuff();

            CreateContent();
        }
        private void CreateContent()
        {
            SkillDef chefCookSkill  = ScriptableObject.CreateInstance<SkillDef>();
            Sprite skillIcon        = Bundle.LoadAsset<Sprite>("TemporarySkillIcon");

            ContentAddition.AddEntityState(typeof(Cook), out _);
            ContentAddition.AddEntityState(typeof(CookingState), out _);

            chefCookSkill.skillName = "ChefReturnsCook";
            chefCookSkill.skillNameToken = TokenPrefix + "COOK_SKILL";
            chefCookSkill.skillDescriptionToken = TokenPrefix + "COOK_SKILL_DESC";

            chefCookSkill.icon = skillIcon;

            chefCookSkill.activationStateMachineName = "Weapon";
            chefCookSkill.activationState = new(typeof(Cook));
            chefCookSkill.isCombatSkill = true;
            chefCookSkill.interruptPriority = EntityStates.InterruptPriority.Frozen;

            chefCookSkill.baseRechargeInterval = 10;
            chefCookSkill.baseMaxStock = 1;
            chefCookSkill.rechargeStock = 1;
            chefCookSkill.requiredStock = 1;
            chefCookSkill.stockToConsume = 1;
            chefCookSkill.mustKeyPress = true;

            chefCookSkill.cancelSprintingOnActivation = true;
            chefCookSkill.forceSprintDuringState = false;
            chefCookSkill.canceledFromSprinting = false;

            chefCookSkill.beginSkillCooldownOnSkillEnd = true;

            ContentAddition.AddSkillDef(chefCookSkill);

            SkillFamily chefSpecialFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_DLC2_Chef.ChefSpecialFamily_asset).WaitForCompletion();

            HG.ArrayUtils.ArrayAppend(ref chefSpecialFamily.variants, new SkillFamily.Variant { skillDef = chefCookSkill });

            LanguageAPI.Add(TokenPrefix + "COOK_SKILL", "Cook");
            LanguageAPI.Add(TokenPrefix + "COOK_SKILL_DESC", "Rapidly prepare meal out of customers, stunning and dealing 6x50% damage. Slain enemies become tasty temporary meal items. Cannot critical hit. Executes enemies below 10% health.");
        }

        private void SetUpAssets()
        {
            Bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Directory.GetParent(Info.Location)!.ToString(), "chefovercooked"));
            StartCoroutine(Bundle.UpgradeStubbedShadersAsync());
        }
    }
}
