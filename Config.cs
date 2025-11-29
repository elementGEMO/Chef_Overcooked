using BepInEx.Configuration;
using HG;
using UnityEngine;

namespace ChefOvercooked;
public static class PluginConfig
{
    public static ConfigEntry<bool> Set_Default;
    public static ConfigEntry<int> Round_To;

    public static ConfigEntry<int> Attack_Instances;
    public static ConfigEntry<int> Damage_Coefficient;
    public static ConfigEntry<float> Proc_Coefficient;
    public static ConfigEntry<float> Attack_Rate;
    public static ConfigEntry<float> Radius;

    public static ConfigEntry<float> Execute_Threshold;
    public static ConfigEntry<float> Execute_Leniency;

    public static void Init()
    {
        GeneralInit();
        SkillInit();
    }

    private static void GeneralInit()
    {
        string token = "! General !";

        Set_Default = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Default Configs", true,
            "[ True = Resets configs on launch (Except Enable Toggles) | False = Configs can be changed ]\nUseful for when Default Values get updated"
        );
        Round_To = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Item Stat Rounding", 0,
            "[ 0 = Whole | 1 = Tenths | 2 = Hundrenths | 3 = ... ]\nRounds item values to respective decimal point"
        );
    }
    private static void SkillInit()
    {
        string token = "Chef Special - Cook";

        Attack_Instances = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Attack Count", 6,
            "[ # of Times this Skill will deal Damage ]"
        ).PostConfig(MathProcess.Max, 1);

        Damage_Coefficient = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Damage Number", 150,
            "[ #% of Base Damage for this Skill to Deal per Attack ]"
        ).PostConfig(MathProcess.Max, 0);

        Proc_Coefficient = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Proc Coefficient", 1f,
            "[ # Proc Coefficient set per Attack ]"
        ).PostConfig(MathProcess.Max, 0);

        Attack_Rate = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Attack Duration", 1f,
            "[ # Seconds this Attack will last ]"
        ).PostConfig(MathProcess.Max, 0);

        Radius = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Attack Radius", 7.5f,
            "[ # Meters for the Radius of this Skill ]"
        ).PostConfig(MathProcess.Max, 0);

        Execute_Threshold = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Execute Threshold", 0.1f,
            "[ # (0 - 1), converted to % of Execute Health when using this Skill ]"
        ).PostConfig(MathProcess.Max, 0).PostConfig(MathProcess.Min, 1);

        Execute_Leniency = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Execute Leniency", 1f,
            "[ # Seconds extra to Execute when using this Skill ]"
        ).PostConfig(MathProcess.Max, 0);
    }
    public enum MathProcess
    {
        Max,
        Min
    };

    public static ConfigEntry<T> PostConfig<T>(this ConfigEntry<T> config)
    {
        if (Set_Default.Value) config.BoxedValue = config.DefaultValue;

        return config;
    }

    public static ConfigEntry<float> PostConfig(this ConfigEntry<float> config, MathProcess capType, float capNum)
    {
        config = config.PostConfig();
        
        if (capType == MathProcess.Max) config.BoxedValue = Mathf.Max((float)config.BoxedValue, capNum);
        else config.BoxedValue = Mathf.Min((float)config.BoxedValue, capNum);

        return config;
    }

    public static ConfigEntry<int> PostConfig(this ConfigEntry<int> config, MathProcess capType, int capNum)
    {
        config = config.PostConfig();

        if (capType == MathProcess.Max) config.BoxedValue = Mathf.Max((int)config.BoxedValue, capNum);
        else config.BoxedValue = Mathf.Min((int)config.BoxedValue, capNum);

        return config;
    }

    /*
    public static ConfigEntry<float> PostConfig(this ConfigEntry<float> config)
    {
        if (Set_Default.Value) config.BoxedValue = config.DefaultValue;

        return config;
    }
    public static ConfigEntry<int> PostConfig(this ConfigEntry<int> config)
    {
        if (Set_Default.Value) config.BoxedValue = config.DefaultValue;

        return config;
    }
    public static ConfigEntry<bool> PostConfig(this ConfigEntry<bool> config)
    {
        if (Set_Default.Value) config.BoxedValue = config.DefaultValue;

        return config;
    }
    */

    /*
    public static void ResetConfig()
    {
        foreach (ConfigEntryBase entry in SotAPlugin.Instance.Config.GetConfigEntries())
        {
            entry.BoxedValue = entry.DefaultValue;
        }
    }
    */
}