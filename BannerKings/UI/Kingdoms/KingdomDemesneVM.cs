using BannerKings.Managers.Titles;
using BannerKings.Managers.Titles.Governments;
using BannerKings.Managers.Titles.Laws;
using BannerKings.UI.Items;
using Bannerlord.UIExtenderEx.Attributes;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerKings.UI.Kingdoms
{
    public class KingdomDemesneVM : BannerKingsViewModel
    {
        private MBBindingList<DemesneLawVM> laws;
        private MBBindingList<HeirVM> heirs;
        private MBBindingList<TripleStringItemVM> aspects;
        private HeirVM mainHeir;
        private string successionDescription;

        public KingdomDemesneVM(FeudalTitle title, Kingdom kingdom) : base(null, false)
        {
            Title = title;
            Kingdom = kingdom;
            laws = new MBBindingList<DemesneLawVM>();
            Heirs = new MBBindingList<HeirVM>();
            Aspects = new MBBindingList<TripleStringItemVM>();
        }

        public FeudalTitle Title { get; private set; }
        public Kingdom Kingdom { get; private set; }

        public override void RefreshValues()
        {
            base.RefreshValues();
            Laws.Clear();
            Heirs.Clear();
            Aspects.Clear();

            if (Title != null)
            {
                SuccessionDescription = Title.Contract.Succession.Description.ToString();

                bool isKing = Kingdom.Leader == Hero.MainHero && Title.deJure == Hero.MainHero;
                foreach (var law in Title.Contract.DemesneLaws)
                {
                    Laws.Add(new DemesneLawVM(DefaultDemesneLaws.Instance.GetLawsByType(law.LawType)
                        .Where(x => x.IsAdequateForKingdom(Kingdom)).ToList(),
                        law,
                        isKing,
                        OnChange));
                }

                foreach (ContractAspect aspect in Title.Contract.ContractAspects)
                {
                    TextObject hint = TextObject.Empty;
                    if (aspect is ContractRight) hint = (aspect as ContractRight).EffectText;
                    
                    Aspects.Add(new TripleStringItemVM(aspect.AspectType.ToString(),
                        aspect.Name.ToString(),
                        string.Empty,
                        new BasicTooltipViewModel(() => aspect.Description.ToString())
                        ));
                }

                var candidates = BannerKingsConfig.Instance.TitleModel.GetSuccessionCandidates(Kingdom.Leader, Title);
                var explanations = new Dictionary<Hero, ExplainedNumber>();

                foreach (Hero hero in candidates)
                {
                    var explanation = BannerKingsConfig.Instance.TitleModel.GetSuccessionHeirScore(Kingdom.Leader, hero, Title, true);
                    explanations.Add(hero, explanation);
                }

                var sorted = (from x in explanations
                              orderby x.Value.ResultNumber descending
                              select x).Take(6);

                for (int i = 0; i < sorted.Count(); i++)
                {
                    var hero = sorted.ElementAt(i).Key;
                    var exp = sorted.ElementAt(i).Value;
                    if (i == 0)
                    {
                        MainHeir = new HeirVM(hero, exp);
                    }
                    else
                    {
                        Heirs.Add(new HeirVM(hero, exp));
                    }
                }
            }
        }

        private void OnChange(SelectorVM<BKDemesneLawItemVM> obj)
        {
            if (obj.SelectedItem != null)
            {
                var vm = obj.GetCurrentItem();
                var resultLaw = DefaultDemesneLaws.Instance.All.FirstOrDefault(x => x.Equals(vm.DemesneLaw));
                if (resultLaw != null && !resultLaw.Equals(vm.DemesneLaw))
                {
                    InformationManager.ShowInquiry(new InquiryData(new TextObject("{=yAPnOQQQ}Enact Law").ToString(),
                        new TextObject("{=RSWao3jU}Enact the {LAW} law thoughtout the demesne of {TITLE}. The law will be enacted for every title in the hierarchy.\n\nCost: {INFLUENCE} {INFLUENCE_ICON}")
                        .SetTextVariable("LAW", resultLaw.Name)
                        .SetTextVariable("TITLE", Title.FullName)
                        .SetTextVariable("INFLUENCE", resultLaw.InfluenceCost)
                        .SetTextVariable("INFLUENCE_ICON", GameTexts.FindText("str_html_influence_icon"))
                        .ToString(),
                        Hero.MainHero.Clan.Influence >= resultLaw.InfluenceCost,
                        true,
                        GameTexts.FindText("str_selection_widget_accept").ToString(),
                        GameTexts.FindText("str_selection_widget_cancel").ToString(),
                        () =>
                        {
                            Title.EnactLaw(resultLaw, Hero.MainHero);
                            RefreshValues();
                        },
                        () => RefreshValues()));
                }
            }
        }

        [DataSourceMethod]
        private void ChangeGovernment()
        {
            List<InquiryElement> aspects = new List<InquiryElement>(4);
            foreach (var government in DefaultGovernments.Instance.All)
            {
                var decision = GetDecision(government);
                aspects.Add(new InquiryElement(
                    government,
                    new TextObject("{=h3xkjV3r}{NAME} - {SUPPORT}% Support, {INFLUENCE}{INFLUENCE_ICON}")
                    .SetTextVariable("NAME", government.Name)
                    .SetTextVariable("SUPPORT", new KingdomElection(decision).GetLikelihoodForOutcome(0).ToString("0.00"))
                    .SetTextVariable("INFLUENCE", decision.GetProposalInfluenceCost())
                    .SetTextVariable("INFLUENCE_ICON", Utils.TextHelper.INFLUENCE_ICON)
                    .ToString(),
                    null,
                    !government.Equals(Title.Contract.Government) && government.IsKingdomAdequate(Kingdom) &&
                    Clan.PlayerClan.Influence >= decision.GetProposalInfluenceCost(),
                    new TextObject("{=GqVVkmQB}{DESCRIPTION}{newline}{newline}Effects:{newline}{EFFECTS}")
                    .SetTextVariable("DESCRIPTION", government.Description)
                    .SetTextVariable("EFFECTS", government.Effects)
                    .ToString()
                    ));
            }

            ShowOptions(new TextObject("{=NHicWxDK}Governments"),
                new TextObject("{=kCowVbVJ}Governments are a quintessential part of a realm's legal framework. You may propose a change to the government form that will be voted on by the peers. Ruling clans will often strongly disagree with such changes.{newline}{newline}For more information, search for Governments in Encyclopedia."),
                aspects);
        }

        [DataSourceMethod]
        private void ChangeSuccession()
        {
            List<InquiryElement> aspects = new List<InquiryElement>(6);
            foreach (var succession in DefaultSuccessions.Instance.All)
            {
                var decision = GetDecision(succession);
                aspects.Add(new InquiryElement(
                    succession,
                    new TextObject("{=h3xkjV3r}{NAME} - {SUPPORT}% Support, {INFLUENCE}{INFLUENCE_ICON}")
                    .SetTextVariable("NAME", succession.Name)
                    .SetTextVariable("SUPPORT", new KingdomElection(decision).GetLikelihoodForOutcome(0).ToString("0.00"))
                    .SetTextVariable("INFLUENCE", decision.GetProposalInfluenceCost())
                    .SetTextVariable("INFLUENCE_ICON", Utils.TextHelper.INFLUENCE_ICON)
                    .ToString(),
                    null,
                    !succession.Equals(Title.Contract.Succession) && succession.IsKingdomAdequate(Kingdom) &&
                    Clan.PlayerClan.Influence >= decision.GetProposalInfluenceCost(),
                    new TextObject("{=wbTgHcsj}{DESCRIPTION}{newline}{newline}Viable Candidates:{newline}{CANDIDATES}{newline}{newline}Effects:{newline}{EFFECTS}")
                    .SetTextVariable("DESCRIPTION", succession.Description)
                    .SetTextVariable("CANDIDATES", succession.CandidatesText)
                    .SetTextVariable("EFFECTS", succession.ScoreText)
                    .ToString()
                    ));
            }

            ShowOptions(new TextObject("{=r1kpgMX2}Successions"),
                new TextObject("{=9Sc0Y4ET}Successions determine how the realm rulership is passed on the death or end of term of the current ruler. You may propose a change to the succession process that will be voted on by the peers.{newline}{newline}For more information, search for Successions in Encyclopedia."),
                aspects);
        }

        [DataSourceMethod]
        private void ChangeInheritance()
        {
            List<InquiryElement> aspects = new List<InquiryElement>(6);
            foreach (var inheritance in DefaultInheritances.Instance.All)
            {
                var decision = GetDecision(inheritance);
                aspects.Add(new InquiryElement(
                    inheritance,
                    new TextObject("{=h3xkjV3r}{NAME} - {SUPPORT}% Support, {INFLUENCE}{INFLUENCE_ICON}")
                    .SetTextVariable("NAME", inheritance.Name)
                    .SetTextVariable("SUPPORT", new KingdomElection(decision).GetLikelihoodForOutcome(0).ToString("0.00"))
                    .SetTextVariable("INFLUENCE", decision.GetProposalInfluenceCost())
                    .SetTextVariable("INFLUENCE_ICON", Utils.TextHelper.INFLUENCE_ICON)
                    .ToString(),
                    null,
                    !inheritance.Equals(Title.Contract.Inheritance) &&
                    Clan.PlayerClan.Influence >= decision.GetProposalInfluenceCost(),
                    inheritance.Description.ToString()));
            }

            ShowOptions(new TextObject("{=LvRBhq9B}Inheritances"),
                new TextObject("{=W4x9W0mY}Inheritances determine how clan leadership and properties are passed on the death of the clan head. You may propose a change to the inheritance process that will be voted on by the peers.{newline}{newline}For more information, search for Inheritances in Encyclopedia."),
                aspects);
        }

        [DataSourceMethod]
        private void ChangeGender()
        {
            List<InquiryElement> aspects = new List<InquiryElement>(3);
            foreach (var genderLaw in DefaultGenderLaws.Instance.All)
            {
                var decision = GetDecision(genderLaw);
                aspects.Add(new InquiryElement(
                    genderLaw,
                    new TextObject("{=h3xkjV3r}{NAME} - {SUPPORT}% Support, {INFLUENCE}{INFLUENCE_ICON}")
                    .SetTextVariable("NAME", genderLaw.Name)
                    .SetTextVariable("SUPPORT", new KingdomElection(decision).GetLikelihoodForOutcome(0).ToString("0.00"))
                    .SetTextVariable("INFLUENCE", decision.GetProposalInfluenceCost())
                    .SetTextVariable("INFLUENCE_ICON", Utils.TextHelper.INFLUENCE_ICON)
                    .ToString(),
                    null,
                    !genderLaw.Equals(Title.Contract.GenderLaw) &&
                    Clan.PlayerClan.Influence >= decision.GetProposalInfluenceCost(),
                    genderLaw.Description.ToString()));
            }

            ShowOptions(new TextObject("{=W4LWHAKu}Gender Laws"),
                new TextObject("{=P4K7NO2T}Gender laws determine what gender is or not favorable for positions of power and take precedence in clan inheritances. You may propose a change to the gender law process that will be voted on by the peers.{newline}{newline}For more information, search for Gender Laws in Encyclopedia."),
                aspects);
        }

        private void ShowOptions(TextObject title, TextObject description, List<InquiryElement> aspects)
        {
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                title.ToString(),
                description.ToString(),
                aspects,
                true,
                1,
                new TextObject("{=qojc4lfK}Propose").ToString(),
                GameTexts.FindText("str_selection_widget_cancel").ToString(),
                (List<InquiryElement> list) =>
                {
                    ContractAspect aspect = list.First().Identifier as ContractAspect;
                    Kingdom.AddDecision(GetDecision(aspect));
                    
                },
                null,
                Utils.Helpers.GetKingdomDecisionSound()));
        }

        private BKContractChangeDecision GetDecision(ContractAspect aspect)
        {
            FeudalContract contract;
            if (aspect is Government) contract = new FeudalContract(null,
                        null,
                        aspect as Government,
                        Title.Contract.Succession,
                        Title.Contract.Inheritance,
                        Title.Contract.GenderLaw);
            else if (aspect is Succession) contract = new FeudalContract(null,
                null,
                Title.Contract.Government,
                aspect as Succession,
                Title.Contract.Inheritance,
                Title.Contract.GenderLaw);
            else if (aspect is Inheritance) contract = new FeudalContract(null,
                null,
                Title.Contract.Government,
                Title.Contract.Succession,
                aspect as Inheritance,
                Title.Contract.GenderLaw);
            else contract = new FeudalContract(null,
                null,
                Title.Contract.Government,
                Title.Contract.Succession,
                Title.Contract.Inheritance,
                aspect as GenderLaw);

            return new BKContractChangeDecision(Title, contract, Clan.PlayerClan);
        }

        [DataSourceProperty]
        public string GovernmentText => new TextObject("{=CS_government}Government").ToString();

        [DataSourceProperty]
        public string InheritanceText => new TextObject("{=J1fyUpgG}Inheritance").ToString();

        [DataSourceProperty]
        public string GenderLawText => new TextObject("{=AX8ilmpj}Gender Law").ToString();

        [DataSourceProperty]
        public string StructureText => new TextObject("{=suts3tKc}Contract Structure").ToString();

        [DataSourceProperty]
        public string GovernmentName => Title?.Contract.Government.Name.ToString();

        [DataSourceProperty]
        public string SuccessionName => Title?.Contract.Succession.Name.ToString();

        [DataSourceProperty]
        public string InheritanceName => Title?.Contract.Inheritance.Name.ToString();

        [DataSourceProperty]
        public string GenderLawName => Title?.Contract.GenderLaw.Name.ToString();

        [DataSourceProperty]
        public HintViewModel GovernmentHint => new HintViewModel(Title?.Contract.Government.Description);

        [DataSourceProperty]
        public HintViewModel SuccessionHint => new HintViewModel(Title?.Contract.Succession.Description);

        [DataSourceProperty]
        public HintViewModel InheritanceHint => new HintViewModel(Title?.Contract.Inheritance.Description);

        [DataSourceProperty]
        public HintViewModel GenderLawHint => new HintViewModel(Title?.Contract.GenderLaw.Description);

        [DataSourceProperty]
        public string HeirText => new TextObject("{=vArnerHC}Heir").ToString();

        [DataSourceProperty]
        public string SuccessionText => new TextObject("{=rTUgik07}Succession").ToString();

        [DataSourceProperty]
        public string LawsText => new TextObject("{=fE6RYz1k}Laws").ToString();

        [DataSourceProperty]
        public string LawsDescriptionText => new TextObject("{=MbSsFJNY}Demesne Laws may be changed a year after they are issued. Changes are made by the sovereign or through voting by the Peers.").ToString();


        [DataSourceProperty]
        public string SuccessionDescription
        {
            get => successionDescription;
            set
            {
                if (value != successionDescription)
                {
                    successionDescription = value;
                    OnPropertyChangedWithValue(value, "SuccessionDescription");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<TripleStringItemVM> Aspects
        {
            get => aspects;
            set
            {
                if (value != aspects)
                {
                    aspects = value;
                    OnPropertyChangedWithValue(value, "Aspects");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<DemesneLawVM> Laws
        {
            get => laws;
            set
            {
                if (value != laws)
                {
                    laws = value;
                    OnPropertyChangedWithValue(value, "Laws");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<HeirVM> Heirs
        {
            get => heirs;
            set
            {
                if (value != heirs)
                {
                    heirs = value;
                    OnPropertyChangedWithValue(value, "Heirs");
                }
            }
        }

        [DataSourceProperty]
        public HeirVM MainHeir
        {
            get => mainHeir;
            set
            {
                if (value != mainHeir)
                {
                    mainHeir = value;
                    OnPropertyChangedWithValue(value, "MainHeir");
                }
            }
        }
    }
}
