﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BannerKings.Behaviours.Retainer
{
    public class BKRetainerBehavior : BannerKingsBehavior
    {
        private Contract contract;
        private bool freelancer;
        private CharacterObject template;

        public Contract GetContract() => contract;

        public HashSet<CharacterObject> AdequateTroopOptions
        {
            get
            {
                HashSet<CharacterObject> options = new HashSet<CharacterObject>();
                Hero main = Hero.MainHero;
                int minLevel = 6;
                int maxLevel = 6;
                if (main.Level > 26)
                {
                    minLevel = 26;
                    maxLevel = 31;
                }
                else if (main.Level > 21)
                {
                    minLevel = 21;
                    maxLevel = 21;
                }
                else if (main.Level > 16)
                {
                    minLevel = 16;
                    maxLevel = 16;
                }
                else if (main.Level > 11)
                {
                    minLevel = 11;
                    maxLevel = 11;
                }

                if (Hero.OneToOneConversationHero != null)
                {
                    var culture = Hero.OneToOneConversationHero.MapFaction.Culture;
                    List<CharacterObject> forbidden = new List<CharacterObject>();
                    foreach (var clan in Clan.All)
                    {
                        if (clan.Culture != culture) continue;

                        if (clan.BasicTroop != null) forbidden.Add(clan.BasicTroop);
                        if (clan.IsMinorFaction && clan.DefaultPartyTemplate != null)
                        {
                            foreach (var stack in clan.DefaultPartyTemplate.Stacks)
                            {
                                forbidden.Add(stack.Character);
                            }
                        }
                    }
                    forbidden.Add(culture.MilitiaArcher);
                    forbidden.Add(culture.MilitiaSpearman);
                    forbidden.Add(culture.MilitiaVeteranArcher);
                    forbidden.Add(culture.MilitiaVeteranSpearman);

                    foreach (CharacterObject troop in CharacterObject.All)
                    {
                        if (troop.Culture == culture &&
                            troop.Occupation == Occupation.Soldier &&
                            !forbidden.Contains(troop) &&
                            !troop.HiddenInEncylopedia &&
                            troop.Level >= minLevel && troop.Level <= maxLevel)
                        {
                            if (!main.IsFemale && troop.IsFemale) continue;
                            options.Add(troop);
                        }  
                    }   
                }

                return options;
            }
        }

        private void FillTroops(HashSet<CharacterObject> options,
            CharacterObject basicTroop, int minLevel, int maxLevel)
        {
            foreach (CharacterObject target in basicTroop.UpgradeTargets)
            {
                if (target.Level >= minLevel && target.Level <= maxLevel) options.Add(target);
                FillTroops(options, target, minLevel, maxLevel);
            }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        private void OnTick(float dt)
        {
            if (contract != null)
            {
                MobileParty contractor = contract.Contractor.PartyBelongedTo;
                if (contractor != null && contractor.LeaderHero == contract.Contractor)
                {
                    MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
                    if (!PlayerCaptivity.IsCaptive && (dt > 0f || (mapState != null && !mapState.AtMenu)))
                    {
                        if (contractor.IsActive)
                        {
                            PartyBase.MainParty.MobileParty.Position2D = contractor.Position2D;
                        }
                    }
                    else if (PlayerCaptivity.IsCaptive)
                    {

                    }
                }
            }
        }

        private void SetCamera()
        {
            if (contract != null)
            {
                MobileParty contractor = contract.Contractor.PartyBelongedTo;
                if (contractor != null && contractor.LeaderHero == contract.Contractor)
                {
                    MobileParty.MainParty.IsActive = false;
                    PartyBase.MainParty.UpdateVisibilityAndInspected(0f, true);
                    contractor.Party.SetAsCameraFollowParty();
                    contractor.Party.UpdateVisibilityAndInspected(0f, false);
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("bk_retainer_start", 
                "lord_talk_speak_diplomacy_2",
                "bk_retainer_reason",
                "{=!}I would like to join your service, {?CONVERSATION_NPC.GENDER}my lady{?}my lord{\\?}.", 
                () => CharacterObject.OneToOneConversationCharacter.HeroObject != null && 
                CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && 
                CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero == CharacterObject.OneToOneConversationCharacter.HeroObject && 
                !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction && 
                Hero.MainHero.Clan.Kingdom == null && 
                CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.Kingdom != null,
                null);

            starter.AddDialogLine("bk_retainer_reason",
                "bk_retainer_reason",
                "bk_retainer_reason_response",
                "{=!}And why would I take thee, {PLAYER.NAME}, under my banner?",
                () => true,
                delegate ()
                {
                    template = null;
                    freelancer = false;
                });

            starter.AddPlayerLine("bk_retainer_reason_response1",
                "bk_retainer_reason_response",
                "bk_retainer_accepted",
                "{=!}I am an expert fighter, my blade will be yours to command.",
                () => true,
                null,
                100,
                delegate (out TextObject explanation)
                {
                    Hero main = Hero.MainHero;
                    bool skill = main.GetSkillValue(DefaultSkills.OneHanded) >= 75 ||
                    main.GetSkillValue(DefaultSkills.TwoHanded) >= 75 ||
                    main.GetSkillValue(DefaultSkills.Polearm) >= 75;

                    explanation = new TextObject("{=!}You need at least 75 levels in any melee skill.");
                    return skill;
                });

            starter.AddPlayerLine("bk_retainer_reason_response2",
                "bk_retainer_reason_response",
                "bk_retainer_accepted",
                "{=!}My eyes are sharp and my aim is true.",
                () => true,
                null,
                100,
                delegate (out TextObject explanation)
                {
                    Hero main = Hero.MainHero;
                    bool skill = main.GetSkillValue(DefaultSkills.Bow) >= 75 ||
                    main.GetSkillValue(DefaultSkills.Crossbow) >= 75 ||
                    main.GetSkillValue(DefaultSkills.Throwing) >= 75;

                    explanation = new TextObject("{=!}You need at least 75 levels in any ranged skill.");
                    return skill;
                });

            starter.AddPlayerLine("bk_retainer_reason_response3",
                "bk_retainer_reason_response",
                "bk_retainer_accepted",
                "{=!}My loyalty and word are not fickle. I give you my unwavering alliegance.",
                () => true,
                null,
                100,
                delegate (out TextObject explanation)
                {
                    Hero main = Hero.MainHero;
                    explanation = new TextObject("{=!}You must be known for a positive sense of Honor.");
                    return main.GetTraitLevel(DefaultTraits.Honor) > 0;
                });

            starter.AddPlayerLine("bk_retainer_reason_response4",
                "bk_retainer_reason_response",
                "bk_retainer_accepted",
                "{=!}I am known for good service in the {REALM}.",
                () => true,
                null,
                100,
                delegate (out TextObject explanation)
                {
                    IFaction faction = Hero.OneToOneConversationHero.MapFaction;
                    MBTextManager.SetTextVariable("REALM", faction.Name);
                    Hero main = Hero.MainHero;
                    explanation = new TextObject("{=!}You must have a friendly notable in the {REALM}.")
                    .SetTextVariable("REALM", faction.Name);
                    return faction.Settlements.Any(x => x.Notables.Any(x => x.IsFriend(Hero.MainHero)));
                });

            starter.AddDialogLine("bk_retainer_accepted",
                "bk_retainer_accepted",
                "bk_retainer_contract",
                "{=!}Very well, {PLAYER.NAME}. And how dost thou wish to serve me?", 
                () => true, 
                null);

            starter.AddPlayerLine("bk_retainer_contract",
                "bk_retainer_contract",
                "bk_retainer_troop_options",
                "{=!}As your servant.",
                () => true,
                () => 
                {
                    freelancer = false;
                    ConversationSentence.SetObjectsToRepeatOver(AdequateTroopOptions.ToList(), 6);
                },
                100,
                delegate (out TextObject explanation)
                {
                    Hero main = Hero.MainHero;
                    explanation = new TextObject("{=!}As a servant, you serve as a troop. Your equipment and wage are on par with standard troops, and you may be promoted with time and good service.");
                    return AdequateTroopOptions.Count > 0;
                });

            starter.AddPlayerLine("bk_retainer_contract",
                "bk_retainer_contract",
                "bk_retainer_finish",
                "{=!}As a freelancer.",
                () => true,
                () => freelancer = true,
                100,
                delegate (out TextObject explanation)
                {
                    Hero main = Hero.MainHero;
                    explanation = new TextObject("{=!}As a freelancer, you serve as a companion. Your wage is determined by your qualifications and your equipment is your responsibility.");
                    return true;
                });

            starter.AddDialogLine("bk_retainer_troop_options",
                "bk_retainer_troop_options",
                "bk_retainer_troop_options_select",
                "{=!}What position?",
                () =>
                {
                    return true;
                },
                null);

            starter.AddRepeatablePlayerLine("bk_retainer_troop_options_select",
                "bk_retainer_troop_options_select",
                "bk_retainer_finish",
                "{=!}{TROOP} - Level: ({LEVEL}), Upgrades: ({UPGRADES})",
                "A different option",
                "bk_retainer_troop_options",
                delegate
                {
                    var troop = ((CharacterObject)ConversationSentence.CurrentProcessedRepeatObject);
                    ConversationSentence.SelectedRepeatLine.SetTextVariable("TROOP", troop.Name);
                    ConversationSentence.SelectedRepeatLine.SetTextVariable("LEVEL", troop.Level);
                    ConversationSentence.SelectedRepeatLine.SetTextVariable("UPGRADES", troop.UpgradeTargets.Length);
                    return true;
                },
                () =>
                {
                    template = (CharacterObject)ConversationSentence.SelectedRepeatObject;
                });

            starter.AddDialogLine("bk_retainer_finish",
                "bk_retainer_finish",
                "bk_retainer_proposal",
                "{=!}Very well, {PLAYER.NAME}. {CONTRACT_TEXT} {TIME_TEXT} {FIELTY_TEXT}",
                () => 
                {
                    TextObject text;
                    contract = new Contract(Hero.OneToOneConversationHero, freelancer, template);
                    if (freelancer)
                    {
                        text = new TextObject("{=!}As a freelancer, I offer thee {HIRING}{GOLD_ICON} immediatly for thy service, as well as {WAGE}{GOLD_ICON} on a daily basis, adjusted regularly and accordingly to thy skills.")
                        .SetTextVariable("WAGE", contract.Wage)
                        .SetTextVariable("HIRING", contract.HiringCost);
                    }
                    else
                    {
                        text = new TextObject("{=!}As my servant, under the role of {TROOP}, I offer thee {HIRING}{GOLD_ICON} immediatly for thy service, as well as {WAGE}{GOLD_ICON} on a daily basis, to be adjusted if and when thou deservest betterment.")
                        .SetTextVariable("TROOP", template.Name)
                        .SetTextVariable("WAGE", contract.Wage)
                        .SetTextVariable("HIRING", contract.HiringCost);
                    }

                    MBTextManager.SetTextVariable("CONTRACT_TEXT", text);
                    MBTextManager.SetTextVariable("TIME_TEXT", new TextObject("{=!}This contract will hold until {DATE}, a year from now, given no extraordinary circunstances. At that time, thou mayest renew it with me.")
                        .SetTextVariable("DATE", CampaignTime.YearsFromNow(1f).ToString()));
                    MBTextManager.SetTextVariable("FIELTY_TEXT", new TextObject("{=!}Know that thy utmost loyalty is expected. If our covenant were to be violated, know that I shall sentence and punish thee under the law."));

                    return true;
                },
                null);

            starter.AddPlayerLine("bk_retainer_proposal",
                "bk_retainer_proposal",
                "bk_retainer_proposal_accepted",
                "{=!}I accept your terms, and swear to you my loyalty.",
                () => true,
                () =>
                {
                    StartService(freelancer);
                    GiveGoldAction.ApplyBetweenCharacters(Hero.OneToOneConversationHero,
                        Hero.MainHero,
                        contract.HiringCost);
                },
                100,
                (out TextObject reason) =>
                {
                    reason = new TextObject("{=!}{HERO} needs to have at least {HIRING} in money to pay you.")
                    .SetTextVariable("HERO", Hero.OneToOneConversationHero.Name)
                    .SetTextVariable("HIRING", contract.HiringCost);
                    return Hero.OneToOneConversationHero.Gold >= contract.HiringCost;
                });

            starter.AddPlayerLine("bk_retainer_proposal",
                "bk_retainer_proposal",
                "bk_retainer_proposal_accepted",
                "{=!}I swear to you my loyalty, and ask no immediate pay.",
                () => true,
                () =>
                {
                    StartService(freelancer);
                });

            starter.AddPlayerLine("bk_retainer_proposal",
                "bk_retainer_proposal",
                "lord_talk_speak_diplomacy_2",
                "{=!}I must refuse, for now.",
                () => true,
                () => contract = null);

            starter.AddDialogLine("bk_retainer_proposal_accepted",
               "bk_retainer_proposal_accepted",
               "close_window",
               "{=!}Then I bid thee welcome, {PLAYER.NAME}.",
               () =>
               {
                   TextObject text;
                   contract = new Contract(Hero.OneToOneConversationHero, freelancer, template);
                   if (freelancer)
                   {
                       text = new TextObject("{=!}As a freelancer, I offer thee {HIRING}{GOLD_ICON} immediatly for thy service, as well as {WAGE}{GOLD_ICON} on a daily basis, adjusted regularly and accordingly to thy skills.")
                       .SetTextVariable("WAGE", contract.Wage)
                       .SetTextVariable("HIRING", contract.HiringCost);
                   }
                   else
                   {
                       text = new TextObject("{=!}As my servant, under the role of {TROOP}, I offer thee {HIRING}{GOLD_ICON} immediatly for thy service, as well as {WAGE}{GOLD_ICON} on a daily basis, to be adjusted if and when thou deservest betterment.")
                       .SetTextVariable("TROOP", template.Name)
                       .SetTextVariable("WAGE", contract.Wage)
                       .SetTextVariable("HIRING", contract.HiringCost);
                   }

                   MBTextManager.SetTextVariable("CONTRACT_TEXT", text);
                   MBTextManager.SetTextVariable("TIME_TEXT", new TextObject("{=!}This contract will hold until {DATE}, a year from now, given no extraordinary circunstances. At that time, thou mayest renew it with me.")
                       .SetTextVariable("DATE", CampaignTime.YearsFromNow(1f).ToString()));
                   MBTextManager.SetTextVariable("FIELTY_TEXT", new TextObject("{=!}Know that thy utmost loyalty is expected. If our covenant were to be violated, know that I shall sentence and punish thee under the law."));

                   return true;
               },
               null);
        }

        private void StartService(bool freelancer)
        {
            MobileParty newParty = Hero.OneToOneConversationHero.PartyBelongedTo;
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }

            if (MobileParty.MainParty.CurrentSettlement != null)
            {
                LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }

            if (MobileParty.MainParty.Army != null)
            {
                if (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
                {
                    DisbandArmyAction.ApplyByLeaderPartyRemoved(MobileParty.MainParty.Army);
                }
                MobileParty.MainParty.Army = null;
            }

            newParty.Party.AddMember(Hero.MainHero.CharacterObject, 1);
            MobileParty.MainParty.ChangePartyLeader(null);
            MobileParty.MainParty.MemberRoster.RemoveTroop(Hero.MainHero.CharacterObject, 
                1, 
                default(UniqueTroopDescriptor), 
                0);
            contract = new Contract(Hero.OneToOneConversationHero, false);
            SetCamera();
        }
    }
}