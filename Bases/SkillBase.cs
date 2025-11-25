using R2API;
using RoR2;
using System.ComponentModel;
using System;
using UnityEngine;
using RoR2.Skills;
using EntityStates;
using System.Collections.Generic;

namespace ChefOvercooked;
public abstract class SkillBase : GenericBase<SkillDef>
{

    protected virtual string DisplaySkillName   => null;
    protected virtual string SkillDescription   => null;
    protected virtual Sprite SkillSprite        => null;

    protected virtual InterruptPriority InterruptPriority           => default;
    protected virtual string ActivationStateMachine                 => null;
    protected virtual SerializableEntityStateType ActivationState   => default;
    protected virtual bool IsCombatSkill                            => default;
    protected virtual bool MustKeyPress                             => default;

    protected virtual bool CancelSprintOnActivation => default;
    protected virtual bool ForceSprintDuringState   => default;
    protected virtual bool CanceledFromSprinting    => default;

    protected virtual float BaseCooldown    => default;
    protected virtual int BaseMaxStock      => default;
    protected virtual int RechargeStock     => default;
    protected virtual int RequiredStock     => default;
    protected virtual int StockToConsume    => default;

    protected override void Create()
    {
        Value = ScriptableObject.CreateInstance<SkillDef>();

        Value.skillName = Name;
        Value.skillNameToken = HelperLanguage.LanguageAdd(ChefOverCookedPlugin.TokenPrefix + Name.ToUpper() + "_NAME", DisplaySkillName);
        Value.skillDescriptionToken = HelperLanguage.LanguageAdd(ChefOverCookedPlugin.TokenPrefix + Name.ToUpper() + "_DESC", SkillDescription);

        Value.icon = SkillSprite;

        Value.interruptPriority = InterruptPriority;
        Value.activationStateMachineName = ActivationStateMachine;
        Value.activationState = ActivationState;
        Value.isCombatSkill = IsCombatSkill;
        Value.mustKeyPress = MustKeyPress;

        Value.cancelSprintingOnActivation = CancelSprintOnActivation;
        Value.forceSprintDuringState = ForceSprintDuringState;
        Value.canceledFromSprinting = CanceledFromSprinting;

        Value.baseRechargeInterval = BaseCooldown;
        Value.baseMaxStock = BaseMaxStock;
        Value.rechargeStock = RechargeStock;
        Value.requiredStock = RequiredStock;
        Value.stockToConsume = StockToConsume;

        ContentAddition.AddSkillDef(Value);
    }
    protected virtual void LogDisplay() { }
}