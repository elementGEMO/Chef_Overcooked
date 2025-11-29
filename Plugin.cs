using R2API;
using BepInEx;
using System.IO;
using UnityEngine;
using ShaderSwapper;
using RoR2.ExpansionManagement;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using UnityEngine.AddressableAssets;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ChefOvercooked
{
    [BepInDependency(ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ChefOverCookedPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginCreator + "." + PluginName;
        public const string PluginCreator = "noodleGemo";
        public const string PluginName = "Chef_Overcooked";
        public const string PluginVersion = "1.0.0";

        public static ExpansionDef AlloyedCollective { get; private set; }
        public static ChefOverCookedPlugin Instance { get; private set; }
        public static AssetBundle Bundle { get; private set; }

        public static readonly string TokenPrefix = "GEMO_CHEF_OVERCOOKED_";

        public void Awake()
        {
            SetUpAssets();

            PluginConfig.Init();
            Log.Init(Logger);

            CreateContent();
        }

        private void CreateContent()
        {
            new AllSounds();

            CreateItems();
            CreateBuffs();

            CookState.CreateEffects();
            CookingState.CreateEffects();

            new CookDamageType();
            new SpecialCookSkill();
        }

        private void CreateItems()
        {
            new MonsterMeatItem();
            new PrimitiveClawsItem();
        }
        private void CreateBuffs()
        {
            new MeatTimerBuff();
            new CookingBuff();
            new CookedBuff();

            new DamageOnBleedBuff();
        }

        private void SetUpAssets()
        {
            Instance = this;
            AlloyedCollective = Addressables.LoadAssetAsync<ExpansionDef>(RoR2_DLC3.DLC3_asset).WaitForCompletion();
            Bundle = AssetBundle.LoadFromFile(Path.Combine(Directory.GetParent(Info.Location)!.ToString(), "chefovercooked"));
            StartCoroutine(Bundle.UpgradeStubbedShadersAsync());
        }
    }
}
