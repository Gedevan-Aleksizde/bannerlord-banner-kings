﻿using BannerKings.Managers.Education.Languages;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BannerKings.Managers.Education.Books
{
    public class BookType
    {
        private ItemObject bookItem;
        private TextObject description;
        private Language language;
        private SkillObject skill;
        private BookUse bookUse;

        public BookType(ItemObject bookItem, TextObject description, Language language, BookUse bookUse, SkillObject skill = null)
        {
            this.bookItem = bookItem;
            this.description = description;
            this.language = language;
            this.bookUse = bookUse;
            this.skill = skill;
        }

        public void FinishBook(Hero hero)
        {
            if (skill == null) return;

            if (bookUse == BookUse.Skillbook) hero.AddSkillXp(skill, 1500f);
            else
            {
                hero.AddSkillXp(skill, 300f);
                hero.HeroDeveloper.AddFocus(skill, 1);
            }
        }

        public ItemObject Item => bookItem;
        public Language Language => language;
        public TextObject Description => description;
        public SkillObject Skill => skill;
        public BookUse Use => bookUse;
    }

    public enum BookUse
    {
        Skillbook,
        Focusbook,
        Dictionary
    }
}