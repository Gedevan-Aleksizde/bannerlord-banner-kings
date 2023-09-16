using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Institutions.Religions.Faiths.Northern
{
    public class TreeloreFaith : PolytheisticFaith
    {
        public override Settlement FaithSeat => Settlement.All.First(x => x.StringId == "town_S3");
        public override TextObject GetDescriptionHint()
        {
            return new TextObject("{=!}Pérkkenukos is a native faith of the Calradian continent, natural to the Vakken peoples, who stretch from the Kachyar peninsula to the Chertyg mountains. Once, they say, there was naught but the sea and sky. High above reigned Pérkken, god of sky and thunder, and deep below, Tursas, king of seas.{newline}Pérkkenukos, the faith in Pérkken, represents such oral Vakken traditions, passed on through tribes and generations, often with nuances of local folklore and beliefs, but ultimately united by a common cultural tradition.");
        }
        public override Banner GetBanner() => new Banner("1.22.22.1836.1836.764.764.1.0.0.203.2.116.427.427.764.638.0.0.0.405.142.116.209.209.764.914.0.0.30.405.116.116.209.209.764.914.0.0.0");

        public override bool IsCultureNaturalFaith(CultureObject culture)
        {
            if (culture.StringId == "vakken")
            {
                return true;
            }

            return false;
        }
        public override bool IsHeroNaturalFaith(Hero hero) => IsCultureNaturalFaith(hero.Culture);

        public override TextObject GetBlessingAction()
        {
            return new TextObject("{=qYmkFsWj}I would like to pledge myself to a cult.");
        }

        public override TextObject GetBlessingActionName()
        {
            return new TextObject("{=bd7HQSJH}pledge an oath to.");
        }

        public override TextObject GetBlessingConfirmQuestion()
        {
            return new TextObject("{=1wZntvX3}Confirm it, {?PLAYER.GENDER}sister{?}brother{\\?} of the forest, and it will be done.");
        }

        public override TextObject GetBlessingQuestion()
        {
            return new TextObject("{=!}To whom would you pledge? The Thunder-Wielder Pérkken, or?");
        }

        public override TextObject GetBlessingQuickInformation()
        {
            return new TextObject("{=j1U1juRf}{HERO} has pledged an oath to {DIVINITY}.");
        }

        public override TextObject GetClergyForbiddenAnswer(int rank)
        {
            return new TextObject("{=AZf5AS94}What is forbidden? Not upholding family and tradition are crimes in the eyes of the gods. Likewise, betraying in act or word those you take as guests. But most of all, cravenness, of course. A craven is no child of forest, much like spoiled fruit is no nourishment. Fight with boldness or do not fight at all.");
        }

        public override TextObject GetClergyForbiddenAnswerLast(int rank)
        {
            return new TextObject("{=jcLBX8Db}The struggle between righteousness and darkness is the nature of the worlds. Our world, that of mankind, lies right in its midst. Uphold all that is godly, or rot in darkness.");
        }

        public override TextObject GetClergyGreeting(int rank) => new TextObject("{=zSR2NTfA}Hail to you, foreigner. Know that the spirits watch over all mankind, true believers or otherwise, and that no malice escapes their senses, and no misdeed goes unrecorded by the oak grain.");

        public override TextObject GetClergyGreetingInducted(int rank) => new TextObject("{=4FBPYfAH}Hail to you, brethren. How can I help you? Do you wish to hear the truth of the gods, or, perhaps, foretell the future?");

        public override TextObject GetClergyInduction(int rank)
        {
            var induction = GetInductionAllowed(Hero.MainHero, rank);
            if (!induction.Item1)
            {
                return new TextObject("{=e4mTQb3c}Alas, one born outside the embrace of the gods, can not choose to be embraced. Though one can be respected for their boldness, only a child of the forest can follow the path of the true gods - it is written in our ancestry. Such is the tale written in the oak grain.[if:convo_bored]");
            }

            return new TextObject("{=!}I ask of you only this - why have you not come before? My brethren of the woods, you have come to your home. The way of Pérkken, that is your true nature. Your blood and bone.");
        }

        public override TextObject GetClergyInductionLast(int rank)
        {
            var induction = GetInductionAllowed(Hero.MainHero, rank);
            if (!induction.Item1)
            {
                return new TextObject("{=x2BRcZSr}Go now, and return to those of your kind, wherever they might be. The children of the forest only accept those born in it.[rf:convo_bored]");
            }

            return new TextObject("{=3eB4RvfE}Be welcome as a child of the forest. Defend your brethren and your gods - fight our enemies fiercely, but also be kind to those that visit your hearth. Do not try and convince them of our ways - it is not their place. Yet it is ours to keep unharmed.[if:convo_excited]");
        }

        public override TextObject GetClergyPreachingAnswer(int rank) => new TextObject("{=cDgdXvLX}When it comes to the gods, there is nothing the words of man can say that the rustling of leaves or burbling of rivers does not tell better. The forest, offspring of the Great Oak, holds the chronicles of the past. Truth, you see, is embedded within all that which is godly, be it the river flow or the oak grain. I merely interpret it.");

        public override TextObject GetClergyPreachingAnswerLast(int rank) => new TextObject("{=Zz2xgYgb}But if you insist... We preach the way of the Thunder Wielder. Valor in defending your ancestry, honor in keeping your word, and generosity towards those you take into your hearth.");

        public override TextObject GetClergyProveFaith(int rank)
        {
            return new TextObject("{=!}Naturally, to follow the way of Pérkken is to be a true faithful. Uphold honor, honesty and righteousness. Defend your brethren and the forest, our home, with valor. Those you take into your hearth as guests - treat them as your own blood, as much as good sense allows.");
        }

        public override TextObject GetClergyProveFaithLast(int rank)
        {
            return new TextObject("{=m5Zva8LP}Most importantly, uphold tradition and ancestry as a child of the forest, remain true to your family and multiply, lest the Great Oak is unrooted by darkness.");
        }

        public override TextObject GetFaithDescription() => new TextObject("{=!}Pérkkenukos is a native faith of the Calradian continent, natural to the Vakken peoples, who stretch from the Kachyar peninsula to the Chertyg mountains. Once, they say, there was naught but the sea and sky. High above reigned Pérkken, god of sky and thunder, and deep below, Tursas, king of seas. A Great Oak once sprang, blocking all sun and moon light from land and sea. Tursas, envious of the oak's heights, set it ablaze. From its ashes, the forests grew, which the Vakken now protect.{newline}Pérkkenukos, the faith in Pérkken, represents such oral Vakken traditions, passed on through tribes and generations, often with nuances of local folklore and beliefs, but ultimately united by a common cultural tradition. As the Vakken are often isolationists, living deep in the woods, their faith is also descentralized and not represented by an organized clergy. However, they all agree on the hallowed status of the region of Omor, a traditional forest-shrine.");

        public override TextObject GetFaithName() => new TextObject("{=!}Pérkkenukos");
        public override string GetId() => "treelore";

        public override int GetIdealRank(Settlement settlement)
        {
            if (settlement.IsVillage)
            {
                return 1;
            }

            return 0;
        }

        public override (bool, TextObject) GetInductionAllowed(Hero hero, int rank)
        {
            if (IsCultureNaturalFaith(hero.Culture))
            {
                return new(true, new TextObject("{=GAuAoQDG}You will be converted"));
            }

            return new(false, new TextObject("{=8k60TAmt}The {FAITH} only accepts those of {STURGIA} and {VAKKEN} cultures")
                .SetTextVariable("FAITH", GetFaithName())
                .SetTextVariable("STURGIA", Utils.Helpers.GetCulture("sturgia").Name)
                .SetTextVariable("VAKKEN", Utils.Helpers.GetCulture("vakken").Name));
        }

        public override int GetMaxClergyRank() => 1;

        public override TextObject GetRankTitle(int rank) => new TextObject("{=Yc1bVZ7a}Elder");

        public override TextObject GetCultsDescription() => new TextObject("{=J4D4X2XJ}Cults");

        public override TextObject GetInductionExplanationText() => new TextObject("{=8k60TAmt}The {FAITH} only accepts those of {STURGIA} and {VAKKEN} cultures")
                .SetTextVariable("FAITH", GetFaithName())
                .SetTextVariable("STURGIA", Utils.Helpers.GetCulture("sturgia").Name)
                .SetTextVariable("VAKKEN", Utils.Helpers.GetCulture("vakken").Name);
    }
}
