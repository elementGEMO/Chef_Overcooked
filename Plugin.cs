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
        public const string PluginVersion = "1.2.2";

        public static ExpansionDef AlloyedCollective { get; private set; }
        public static ChefOverCookedPlugin Instance { get; private set; }
        public static AssetBundle Bundle { get; private set; }

        public static readonly string TokenPrefix = "GEMO_CHEF_OVERCOOKED_";

        public void Awake()
        {
            // connect localhost:7777
            // On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };

            SetUpAssets();

            PluginConfig.Init();
            Log.Init(Logger);

            CreateContent();
            RecipeCatalogChef.Init();
        }

        private void CreateContent()
        {
            new ItemReRender();
            new AllSounds();

            CreateItems();
            CreateEquipments();
            CreateBuffs();

            CookState.CreateEffects();
            CookingState.CreateEffects();
            StalkState.CreateEffects();

            new CookDamageType();
            new SpecialCookSkill();
        }

        private void CreateItems()
        {
            // Regular Items
            new PrimitiveClawsItem();
            new WarpingStalkItem();

            // Food / Meal Items
            new MonsterMeatItem();

            new GrilledLizardKebabItem();
            new GolemEssenceTwistItem();
            new GlowingJellyPastaItem();
            new BloodCandiedEyeItem();
            new CarbonatedStormItem();
        }

        private void CreateEquipments()
        {
            new SummonChefEquip();
            new ConsumedChefEquip();
        }
        private void CreateBuffs()
        {
            new MeatTimerBuff();
            new CookingBuff();
            new CookedBuff();

            new DamageOnBleedBuff();
            new SpectralBuff();
            new StalkBuff();
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
