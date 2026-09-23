using BepInEx.Configuration;
using HG;
using UnityEngine;

namespace ChefOvercooked;
public static class PluginConfig
{
    // General Configs
    public static ConfigEntry<bool> Set_Default;
    public static ConfigEntry<int> Round_To;

    // Chef Special Configs
    public static ConfigEntry<int> Attack_Instances;
    public static ConfigEntry<int> Damage_Coefficient;
    public static ConfigEntry<float> Proc_Coefficient;
    public static ConfigEntry<float> Attack_Rate;
    public static ConfigEntry<float> Radius;

    public static ConfigEntry<float> Execute_Threshold;
    public static ConfigEntry<float> Execute_Leniency;
    public static ConfigEntry<float> Temp_Duration;

    // Item Configs

    // Primitive Claws
    public static ConfigEntry<float> Claw_Base_Bleed;
    public static ConfigEntry<float> Claw_Damage_Stack;
    public static ConfigEntry<int> Claw_Stack_Cap;
    public static ConfigEntry<int> Claw_Stack_Increase;

    public static void Init()
    {
        GeneralInit();
        SkillInit();
        ItemInit();
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

        Temp_Duration = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Temp Food Duration", 0.333f,
            "[ #, converted to % for how long Temporary Food items should be from this Skill ]"
        ).PostConfig(MathProcess.Max, 0);
    }
    private static void ItemInit()
    {
        string clawsToken = "Item - Primitive Claws";

        Claw_Base_Bleed = ChefOverCookedPlugin.Instance.Config.Bind(
            clawsToken, "Bleed Chance", 5f,
            "[ #% Bleed Chance ]"
        ).PostConfig(MathProcess.Max, 0);

        Claw_Damage_Stack = ChefOverCookedPlugin.Instance.Config.Bind(
            clawsToken, "Damage per Buff", 8f,
            "[ #% Damage Increase for each stack from inflicting Bleed ]"
        ).PostConfig(MathProcess.Max, 0);

        Claw_Stack_Cap = ChefOverCookedPlugin.Instance.Config.Bind(
            clawsToken, "Base Buff Cap", 3,
            "[ # of Max Buffs from inflicting Bleed ]"
        ).PostConfig(MathProcess.Max, 0);

        Claw_Stack_Increase = ChefOverCookedPlugin.Instance.Config.Bind(
            clawsToken, "Buff Cap Stack", 2,
            "[ # of Max Buffs added per single item stack ]"
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