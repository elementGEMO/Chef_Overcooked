using RoR2.Skills;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2BepInExPack.GameAssetPathsBetter;
using R2API;
using EntityStates;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using ChefOvercooked.Achievements;

namespace ChefOvercooked;
using static HelperFontColor;
using static HelperLanguage;
using static StringHelper;
public class SpecialScepterCookSkill : SkillBase
{
    public static SkillDef SkillDef;
    protected override string Name => "ScepterCookSkill";

    protected override string DisplaySkillName  => "Buffet";
    protected override string SkillDescription => FuseText([
        string.Format("Stunning".Style(FontColor.cIsDamage) + ". Rapidly " + "Cleave ".Style(FontColor.cIsDamage) + "enemies for " + "{0}x{1}% ".Style("#d299ff") + "damage".Style(FontColor.cIsDamage) + ". Slain enemies become " + "temporary Meal items".Style(FontColor.cIsUtility) + ".",
            PluginConfig.Attack_Instances.Value * PluginConfig.Attack_Instances_Scepter.Value, PluginConfig.Damage_Coefficient.Value * PluginConfig.Damage_Coefficient_Scepter.Value),

        string.Format(" Range is increased with attack speed".Style("#d299ff") + ".")
    ]);

    //#d299ff

    protected override Sprite SkillSprite   => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("ScepterSkillBuffetIcon");

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

    protected override void Initialize() => SkillDef = Value;
}
