﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using BannerKings.Populations;
using TaleWorlds.SaveSystem;
using BannerKings.Managers.Titles;
using System;
using BannerKings.Managers.Institutions.Religions;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Court
{
    public class CouncilData : BannerKingsData
    {
        [SaveableProperty(1)]
        private Clan clan { get; set; }

        [SaveableProperty(2)]
        private List<CouncilMember> members { get; set; }

        [SaveableProperty(3)]
        private List<CouncilMember> royalMembers { get; set; }

        public CouncilData(Clan clan, Hero marshall = null, Hero chancellor = null, Hero steward = null, Hero spymaster = null,
            Hero spiritual = null)
        {
            this.clan = clan;
            this.members = new List<CouncilMember>();
            this.members.Add(new CouncilMember(marshall, CouncilPosition.Marshall));
            this.members.Add(new CouncilMember(chancellor, CouncilPosition.Chancellor));
            this.members.Add(new CouncilMember(steward, CouncilPosition.Steward));
            this.members.Add(new CouncilMember(spymaster, CouncilPosition.Spymaster));
            this.members.Add(new CouncilMember(spiritual, CouncilPosition.Spiritual));
            royalMembers = new List<CouncilMember>();
        }

        public Hero Owner => clan.Leader;

        public MBReadOnlyList<CouncilMember> RoyalPositions
        {
            get
            {
                if (royalMembers == null)
                    royalMembers = new List<CouncilMember>();
                return royalMembers.GetReadOnlyList();
            }
        }


        public bool IsRoyal
        {
            get
            {
                Kingdom kingdom = clan.Kingdom;
                if (kingdom == null) return false;

                if (clan.Kingdom.RulingClan != clan)
                    return false;

                FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(kingdom);
                if (sovereign == null) return false;

                return sovereign.deJure == clan.Leader;
            }
        }


        internal override void Update(PopulationData data)
        {

            if (royalMembers == null) royalMembers = new List<CouncilMember>();

            foreach (CouncilMember member in this.members)
            {
                if (member.Member != null && (member.Member.IsDead || member.Member.IsDisabled))
                    member.Member = null;
            }

            foreach (CouncilMember member in this.royalMembers)
            {
                if (member.Member != null && (member.Member.IsDead || member.Member.IsDisabled))
                    member.Member = null;
            }

            if (IsRoyal)
            {
                foreach (CouncilMember position in members)
                    if (!position.IsRoyal)
                    {
                        position.IsRoyal = true;
                        if (position.Member != null && !position.IsValidCandidate(position.Member))
                            position.Member = null;
                    }

                List<CouncilMember> royal = GetIdealRoyalPositions();
                if (royalMembers.Count != royal.Count)
                    foreach (CouncilMember position in royal)
                        if (royalMembers.FirstOrDefault(x => x.Position == position.Position) == null)
                            royalMembers.Add(position);
            }
            else
            {
                if (royalMembers.Count > 0) royalMembers.Clear();
                foreach (CouncilMember position in members)
                    if (position.IsRoyal)
                        position.IsRoyal = false;
            }

            if (MBRandom.RandomInt(1, 100) > 5) return;

            CouncilMember vacant = members.FirstOrDefault(x => x.Member == null);
            if (vacant == null)
            {
                vacant = royalMembers.FirstOrDefault(x => x.Member == null);
                if (vacant == null) return;
            }

            vacant.Member = SelectHeroForPosition(vacant);
        }

        public List<CouncilMember> GetIdealRoyalPositions()
        {
            List<CouncilMember> positions = new List<CouncilMember>();
            if (clan.Kingdom == null) return positions;

            FeudalTitle sovereign = BannerKingsConfig.Instance.TitleManager.GetSovereignTitle(clan.Kingdom);
            if (sovereign == null || sovereign.contract == null) return positions;
            GovernmentType government = sovereign.contract.Government;

            if (government == GovernmentType.Imperial)
                positions.Add(new CouncilMember(null, CouncilPosition.Heir));
            else if (government == GovernmentType.Feudal)
                positions.Add(new CouncilMember(null, CouncilPosition.Constable));

            if (clan.Kingdom.Culture == Utils.Helpers.GetCulture("vlandia"))
                positions.Add(new CouncilMember(null, CouncilPosition.Castellan));

            if (clan.Kingdom.Culture == Utils.Helpers.GetCulture("battania"))
                positions.Add(new CouncilMember(null, CouncilPosition.Elder));



            return positions;
        }

        public List<Hero> GetAvailableHeroes()
        {
            List<Hero> currentMembers = GetMembers();
            List<Hero> available = new List<Hero>();
            foreach (CouncilMember position in members)
            {
                Hero hero = position.Member;
                if (hero == null) continue;
                if (!currentMembers.Contains(hero) && hero.IsAlive && !hero.IsChild)
                    available.Add(hero);
            }
            return available;
        }

        public Hero SelectHeroForPosition(CouncilMember position)
        {
            List<ValueTuple<Hero, float>> list = new List<ValueTuple<Hero, float>>();
            foreach (Hero hero in GetAvailableHeroes())
                if (position.IsValidCandidate(hero))
                    list.Add((hero, GetCompetence(hero, position.Position) + ((float)clan.Leader.GetRelation(hero) * 0.001f)));


            return MBRandom.ChooseWeighted(list);
        }

        public List<Hero> GetCourtMembers()
        {
            List<Hero> heroes = new List<Hero>();

            MBReadOnlyList<Hero> members = clan.Heroes;
            if (members != null && members.Count > 0)
                foreach (Hero member in members)
                    if (member != this.clan.Leader && member.IsAlive && !heroes.Contains(member))
                        heroes.Add(member);

            if (BannerKingsConfig.Instance.TitleManager.IsHeroTitleHolder(Owner))
            {
                List<FeudalTitle> vassals = BannerKingsConfig.Instance.TitleManager.GetVassals(Owner);
                if (vassals != null && vassals.Count > 0)
                    foreach (FeudalTitle vassal in vassals)
                        if (vassal.deJure != this.clan.Leader && !heroes.Contains(vassal.deJure))
                            heroes.Add(vassal.deJure);

                FeudalTitle highest = BannerKingsConfig.Instance.TitleManager.GetHighestTitle(Owner);
                if (highest != null && highest.IsSovereignLevel && clan.Kingdom != null)
                    foreach (Clan clan in clan.Kingdom.Clans)
                        if (clan.Leader != Owner && !heroes.Contains(clan.Leader))
                            heroes.Add(clan.Leader);
            }

            MBReadOnlyList<Town> towns = this.clan.Fiefs;
            if (towns != null && towns.Count > 0)
            {
                Religion rel = BannerKingsConfig.Instance.ReligionsManager.GetHeroReligion(clan.Leader);
                foreach (Town town in towns)
                {
                    MBReadOnlyList<Hero> notables = town.Settlement.Notables;
                    if (notables != null && notables.Count > 0)
                        foreach (Hero notable in notables)
                            if (!heroes.Contains(notable))
                                heroes.Add(notable);

                    if (rel != null && rel.Clergy.ContainsKey(town.Settlement))
                        heroes.Add(rel.Clergy[town.Settlement].Hero);
                }
            }

            return heroes;
        }

        public List<Hero> GetMembers()
        {
            List<Hero> heroes = new List<Hero>();
            foreach (CouncilMember councilMember in this.members)
                if (councilMember.Member != null) heroes.Add(councilMember.Member);

            return heroes;
        }

        public bool IsMember(Hero hero)
        {
            bool member = false;
            foreach (CouncilMember councilMember in this.members)
                if (councilMember.Member == hero)
                {
                    member = true;
                    break;
                }

            return member;
        }

        public float GetCompetence(Hero hero, CouncilPosition position)
        {
            float competence = 0f;
            bool found = false;
            foreach (CouncilMember member in this.members)
                if (member.Member == hero)
                {
                    competence = member.Competence;
                    found = true;
                    break;
                }

            if (!found)
                competence = new CouncilMember(hero, position).Competence;
            return competence;
        }


        public float GetCompetence(CouncilPosition position)
        {
            float competence = 0f;
            foreach (CouncilMember member in this.members)
                if (member.Position == position)
                {
                    competence = member.Competence;
                    break;
                }
            return competence;
        }

        public Hero Marshall
        {
            get => this.members.First(x => x.Position == CouncilPosition.Marshall).Member;
            set => this.members.First(x => x.Position == CouncilPosition.Marshall).Member = value;
        }
        public Hero Chancellor
        {
            get => this.members.First(x => x.Position == CouncilPosition.Chancellor).Member;
            set => this.members.First(x => x.Position == CouncilPosition.Chancellor).Member = value;
        }
        public Hero Steward
        {
            get => this.members.First(x => x.Position == CouncilPosition.Steward).Member;
            set => this.members.First(x => x.Position == CouncilPosition.Steward).Member = value;
        }
        public Hero Spymaster
        {
            get => this.members.First(x => x.Position == CouncilPosition.Spymaster).Member;
            set => this.members.First(x => x.Position == CouncilPosition.Spymaster).Member = value;
        }
        public Hero Spiritual
        {
            get
            {
                CouncilMember position = members.FirstOrDefault(x => x.Position == CouncilPosition.Spiritual);
                if (position == null)
                {
                    position = new CouncilMember(null, CouncilPosition.Spiritual);
                    members.Add(position);
                }

                return position.Member;
            }
            set => this.members.First(x => x.Position == CouncilPosition.Spiritual).Member = value;
        }

        public CouncilMember GetCouncilMember(CouncilPosition position) => members.FirstOrDefault(x => x.Position == position);

        public float AdministrativeCosts
        {
            get
            {
                float costs = 0f;
                foreach (CouncilMember councilMember in members)
                    if (councilMember.Member != null)
                        costs += councilMember.AdministrativeCosts();
                return costs;
            }
        }
    }

    public class CouncilMember
    {
        [SaveableProperty(1)]
        private Hero member { get; set; }

        [SaveableProperty(2)]
        private CouncilPosition position { get; set; }

        [SaveableProperty(3)]
        private bool isRoyal { get; set; }

        public CouncilMember(Hero member, CouncilPosition position)
        {
            this.member = member;
            this.position = position;
        }

        public Hero Member
        {
            get => this.member;
            set => this.member = value;
        }
        public CouncilPosition Position => this.position;
        public bool IsRoyal
        {
            get => isRoyal;
            set => isRoyal = value;
        }

        public TextObject GetName()
        {
            TextObject text = new TextObject("{=!}" + position.ToString());


            return text;
        }

        public bool IsValidCandidate(Hero candidate)
        {
            if (position == CouncilPosition.Spiritual)
            {
                return BannerKingsConfig.Instance.ReligionsManager.IsPreacher(candidate);
            }
            else if (IsCorePosition(position))
                return candidate.IsNoble;

            return true;
        }

        public float AdministrativeCosts()
        {
            float cost = 0.01f;
            if (position == CouncilPosition.Spiritual)
                cost = 0f;
            else if (IsCorePosition(position))
                cost = 0.03f;

            return cost;
        }

        public bool IsCorePosition(CouncilPosition position) => position == CouncilPosition.Marshall || position == CouncilPosition.Steward ||
            position == CouncilPosition.Steward || position == CouncilPosition.Chancellor;

        public float Competence
        {
            get
            {
                if (this.member != null)
                {
                    int targetCap = 300;
                    float primarySkill = 0f;
                    float secondarySkill = 0f;

                    targetCap += 15 * (member.GetAttributeValue(DefaultCharacterAttributes.Intelligence) - 5);
                    if (this.position == CouncilPosition.Marshall)
                    {
                        primarySkill = member.GetSkillValue(DefaultSkills.Leadership);
                        secondarySkill = member.GetSkillValue(DefaultSkills.Tactics);
                    }
                    else if (this.position == CouncilPosition.Chancellor)
                    {
                        primarySkill = member.GetSkillValue(DefaultSkills.Charm);
                        secondarySkill = member.GetSkillValue(DefaultSkills.Charm);
                    }
                    else if (this.position == CouncilPosition.Steward)
                    {
                        primarySkill = member.GetSkillValue(DefaultSkills.Steward);
                        secondarySkill = member.GetSkillValue(DefaultSkills.Trade);
                    }
                    else if (this.position == CouncilPosition.Spymaster)
                    {
                        primarySkill = member.GetSkillValue(DefaultSkills.Roguery);
                        secondarySkill = member.GetSkillValue(DefaultSkills.Scouting);
                    }

                    return MBMath.ClampFloat((primarySkill + (secondarySkill / 2)) / targetCap, 0f, 1f);
                }
                return 0f;
            }
        }
    }

    public enum CouncilPosition
    {
        Marshall,
        Chancellor,
        Steward,
        Spymaster,
        Spiritual,
        Heir,
        Castellan,
        Druzina,
        Elder,
        Constable
    }
}