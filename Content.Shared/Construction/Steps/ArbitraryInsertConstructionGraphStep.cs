using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    public abstract partial class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")] public string Name { get; private set; } = string.Empty;

        [DataField("icon")] public SpriteSpecifier? Icon { get; private set; }

        [DataField("tag")]
        public string? _tag;

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            string? nameLocale = null;
            if (_tag is not null && prototypeManager.TryIndex<TagPrototype>(_tag, out var tag))
            {
                var entities = prototypeManager.EnumeratePrototypes<EntityPrototype>();
                foreach (var item in entities)
                {
                    if (!item.TryGetComponent<TagComponent>(out var entityTag))
                        continue;

                    if (entityTag.Tags.Contains(tag.ID))
                    {
                        nameLocale = item.Name;
                        break;
                    }
                }
            }

            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[]{ ("name", nameLocale ?? Name) },
                Icon = Icon,
                NameLocalization = nameLocale,
            };
        }
    }
}
