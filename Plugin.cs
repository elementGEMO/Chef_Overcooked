using R2API;
using BepInEx;
using System.IO;
using UnityEngine;
using ShaderSwapper;

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

            CreateContent();
        }

        public void CreateContent()
        {
            //new CookState();
            new MonsterMeatItem();
            new MeatTimerBuff();

            new CookDamageType();
            new CookingBuff();
            new CookedBuff();

            new SpecialCookSkill();
        }

        private void SetUpAssets()
        {
            Bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Directory.GetParent(Info.Location)!.ToString(), "chefovercooked"));
            StartCoroutine(Bundle.UpgradeStubbedShadersAsync());
        }
    }
}
