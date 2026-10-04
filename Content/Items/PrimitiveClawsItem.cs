using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChefOvercooked;
using static StringHelper;
using static HelperFontColor;
public class PrimitiveClawsItem : ItemBase
{
    protected override string Name => "PrimitiveClaws";
    public static ItemDef ItemDef;
    protected override CombinedItemTier Tier => ItemTier.Tier2;
    protected override ItemTag[] Tags => [
        ItemTag.AllowedForUseAsCraftingIngredient,
        ItemTag.Damage,
        ItemTag.FoodRelated,
        ItemTag.CanBeTemporary
    ];
    protected override bool IsRemovable => true;

    protected override GameObject PickupModelPrefab => ChefOverCookedPlugin.Bundle.LoadAsset<GameObject>("primitiveClawsModel");
    protected override Sprite PickupIconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texPrimitiveClawsIcon");
    protected override string PickupText => string.Format("Inflicting bleed increases damage. Stacks {0} times.", PluginConfig.Claw_Stack_Cap.Value);
    protected override string Description => FuseText([
        string.Format("Gain " + "{0}% bleed chance".Style(FontColor.cIsDamage) + ". ",
            RoundVal(PluginConfig.Claw_Base_Bleed.Value)),

        string.Format("Inflicting " + "bleed ".Style(FontColor.cIsDamage) + "increases base damage by " + "{0}%".Style(FontColor.cIsDamage) + ". ",
            RoundVal(PluginConfig.Claw_Damage_Stack.Value)),

        string.Format("Maximum cap of " + "{0}% ".Style(FontColor.cIsDamage) + "({1}% per stack) ".Style(FontColor.cStack).OptText(PluginConfig.Claw_Stack_Increase.Value > 0) + "damage.",
            RoundVal(PluginConfig.Claw_Damage_Stack.Value * PluginConfig.Claw_Stack_Cap.Value), RoundVal(PluginConfig.Claw_Damage_Stack.Value * PluginConfig.Claw_Stack_Increase.Value).SignVal())
    ]);
    protected override string Lore => "<style=cMono>//-- INITIALIZING UES BIOLOGICAL SPECIMEN DATABASE. --\n//-- PLEASE ENTER COMMAND. FOR LIST OF COMMANDS, TYPE ''HELP''. --//\n\n></style>new\n\n<style=cMono>>> PLEASE ENTER UNIQUE ENTRY ID...\n\n></style>lemurian claws\n\n<style=cMono>>> 216 ENTRIES FOUND MATCHING NAME: lemurian claws. PLEASE ENTER UNIQUE IDENTIFIER...\n\n></style>lemurian claws 217\n\n<style=cMono>>> PLEASE DESCRIBE SPECIMEN...\n\n></style>One of the grunts handed me these in a biohazard disposal bag, half submerged in blood and leaking all over my desk. Great job on the biohazard containment. Blood came back with 6 different matches. Four of them human, all our guys, one of which was the grunt who gave me the claws. Lovely. \n\nThe even abrasion patterns suggest that all four claws came from the same creature. Claws show significant wear and numerous growth layers, which means that this was likely one of the older, yellow Lemurian variants we've observed. Still sharp as hell, though. I added a 7th identifiable match to that pool of blood because I got too eager pulling them out of the bag. \n\nOne interesting aspect is the specific markings found on the claws. When lining the claws up as they would sit on a lemurian's hand, I've found multiple indents in an arc shape, perhaps bite marks? Let me run it against the database and see what this thing was fighting. Indents come back as... Human teeth.\n\nThink I know what the grunt meant now when he said ''Don't ask how I got 'em''.\n\n<style=cMono>>> ENTRY FINALIZED. PUBLISH? y/n...\n\n></style>y\n\n<style=cMono>>> ENTRY PUBLISHED.</style>";
    // Log provided by @plnk

    protected override string DisplayName => "Primitive Claws";

    protected override void Initialize()
    {
        ItemDef = Value;

        RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
    }

    private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        bool hasItem = sender.inventory ? sender.inventory.GetItemCountEffective(ItemDef) > 0 : false;

        if (hasItem) args.bleedChanceAdd += PluginConfig.Claw_Base_Bleed.Value;
    }

    protected override void LogDisplay()
    {
        ModelPanelParameters modelParam = PickupModelPrefab.AddComponent<ModelPanelParameters>();
        var foundMesh = PickupModelPrefab.transform.GetChild(0);

        if (!foundMesh) return;

        modelParam.focusPointTransform = foundMesh;
        modelParam.cameraPositionTransform = foundMesh;
        modelParam.minDistance = 2f;
        modelParam.maxDistance = 7.5f;
        modelParam.modelRotation = new Quaternion(-0.9999383f, 0, 0, 0.0111104f);
    }
}