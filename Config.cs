using BepInEx.Configuration;
using HG;
using UnityEngine;

namespace ChefOvercooked;
public static class PluginConfig
{
    public enum ItemOptions
    {
        FoodTier,
        Ingredients,
        Both
    }

    // General Configs
    public static ConfigEntry<bool> Set_Default;
    public static ConfigEntry<int> Round_To;

    // Chef Special Configs
    public static ConfigEntry<ItemOptions> FoodDrops;

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

    // Grilled Lizard Kebab
    public static ConfigEntry<float> Kebab_Base_Damage;
    public static ConfigEntry<float> Kebab_Stack_Damage;
    public static ConfigEntry<float> Kebab_Lem_Damage;
    public static ConfigEntry<int> Kebab_Lem_Cap;

    // Goleme Essence with a Twist
    public static ConfigEntry<float> Golem_Percent_Barrier;
    public static ConfigEntry<float> Golem_Flat_Barrier;
    public static ConfigEntry<float> Golem_Base_Armor;
    public static ConfigEntry<float> Golem_Stack_Armor;

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

        FoodDrops = ChefOverCookedPlugin.Instance.Config.Bind(
            token, "Food Items from Skill", ItemOptions.FoodTier,
            "[ Which Items will be temporarily created. Overwritten regardless if Alloyed Collective is disabled ]"
        ).PostConfig();

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
            token, "Temp Food Stack", 0.5f,
            "[ One Temporary Food item is worth # compared to one stack ]"
        ).PostConfig(MathProcess.Max, 0);
    }
    private static void ItemInit()
    {
        string clawsToken = "Item - Primitive Claws";   // Primitive Claws Configs

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

        string kebabToken = "Item - Grilled Lizard Kebab";  // Grilled Lizard Kebab Configs

        Kebab_Base_Damage = ChefOverCookedPlugin.Instance.Config.Bind(
            kebabToken, "Base Damage", 30f,
            "[ #% Damage Dealt increased ]"
        ).PostConfig(MathProcess.Max, 0);

        Kebab_Stack_Damage = ChefOverCookedPlugin.Instance.Config.Bind(
            kebabToken, "Damage per Stack", 15f,
            "[ #% Damage Dealt increased per stack of Item ]"
        ).PostConfig(MathProcess.Max, 0);

        Kebab_Lem_Damage = ChefOverCookedPlugin.Instance.Config.Bind(
            kebabToken, "Lemurian Damage Inherit", 500f,
            "[ #% Damage Inherited for the Lemurian ]"
        ).PostConfig(MathProcess.Max, 0);

        Kebab_Lem_Cap = ChefOverCookedPlugin.Instance.Config.Bind(
            kebabToken, "Lemurian Cap", 1,
            "[ # of Lemurians that can spawn ]"
        ).PostConfig(MathProcess.Max, 0);

        string golemToken = "Item - Golem Essence with a Twist";  // Golem Essence Twist Configs

        Golem_Percent_Barrier = ChefOverCookedPlugin.Instance.Config.Bind(
            golemToken, "Percent Barrier Gain", 30f,
            "[ #% Temporary Barrier gained on Item pickups ]"
        ).PostConfig(MathProcess.Max, 0);

        Golem_Flat_Barrier = ChefOverCookedPlugin.Instance.Config.Bind(
            golemToken, "Flat Barrier Gain", 30f,
            "[ # Temporary Barrier gained on Collectible pickups ]"
        ).PostConfig(MathProcess.Max, 0);

        Golem_Base_Armor = ChefOverCookedPlugin.Instance.Config.Bind(
            golemToken, "Base Armor", 40f,
            "[ # Armor increased ]"
        ).PostConfig(MathProcess.Max, 0);

        Golem_Stack_Armor = ChefOverCookedPlugin.Instance.Config.Bind(
            golemToken, "Armor per Stack", 20f,
            "[ # Armor per Item stack ]"
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