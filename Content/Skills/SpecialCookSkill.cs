using RoR2.Skills;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2BepInExPack.GameAssetPathsBetter;
using R2API;
using EntityStates;
using BepInEx.Configuration;

namespace ChefOvercooked;
using static HelperFontColor;
using static HelperLanguage;
public class SpecialCookSkill : SkillBase
{
    public static SkillDef SkillDef;
    protected override string Name => "CookSkill";

    protected override string DisplaySkillName  => "Cook";
    protected override string SkillDescription => string.Format(
        "Stunning".Style(FontColor.cIsDamage) + ". Rapidly " + "Cleave ".Style(FontColor.cIsDamage) + "enemies for " + "{0}x{1}% damage".Style(FontColor.cIsDamage) + ". Slain enemies become already discovered " + "temporary food items".Style(FontColor.cIsUtility) + ".",
        PluginConfig.Attack_Instances.Value, PluginConfig.Damage_Coefficient.Value
    );

    protected override Sprite SkillSprite       => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("TemporarySkillIcon");

    protected override InterruptPriority InterruptPriority          => InterruptPriority.Frozen;
    protected override string ActivationStateMachine                => "Weapon";
    protected override SerializableEntityStateType ActivationState  => new(typeof(CookState));
    protected override bool IsCombatSkill                           => true;
    protected override bool MustKeyPress                            => true;

    protected override bool CancelSprintOnActivation    => true;
    protected override bool ForceSprintDuringState      => false;
    protected override bool CanceledFromSprinting       => false;

    protected override bool CooldownOnSkillEnd  => true;
    protected override float BaseCooldown       => 10f;
    protected override int BaseMaxStock         => 1;
    protected override int RechargeStock        => 1;
    protected override int RequiredStock        => 1;
    protected override int StockToConsume       => 1;

    protected override void Initialize()
    {
        SkillDef = Value;

        SkillDef.keywordTokens = [
            "KEYWORD_STUNNING",
            LanguageAdd(
                ChefOverCookedPlugin.TokenPrefix + "KEYWORD_CLEAVE",
                string.Format("Cleave".Style(FontColor.cKeywordName) + "Instantly kills enemies below ".Style(FontColor.cSub) + "{0}% health".Style(FontColor.cIsHealth) + ".".Style(FontColor.cSub), PluginConfig.Execute_Threshold.Value * 100)
            )
        ];

        ContentAddition.AddEntityState(typeof(CookState), out _);
        ContentAddition.AddEntityState(typeof(CookingState), out _);

        SkillFamily chefSpecialFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_DLC2_Chef.ChefSpecialFamily_asset).WaitForCompletion();
        HG.ArrayUtils.ArrayAppend(ref chefSpecialFamily.variants, new SkillFamily.Variant { skillDef = SkillDef });
    }
}
