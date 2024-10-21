using Content.Shared.Examine;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Steps
{
    public abstract partial class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")] public string Name { get; private set; } = string.Empty;

        [DataField("icon")] public SpriteSpecifier? Icon { get; private set; }

        [DataField("tag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
        public string? Tag { get; private set; }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (string.IsNullOrEmpty(Name))
                return;

            examinedEvent.PushMarkup(Loc.GetString("construction-insert-arbitrary-entity", ("stepName", Name)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            string? nameLocale = null;

            if (Tag is not null && prototypeManager.TryIndex<TagPrototype>(Tag, out var tag))
            {
                var entities = prototypeManager.EnumeratePrototypes<EntityPrototype>();

                foreach (var item in entities)
                {
                    if (item.TryGetComponent<TagComponent>(out var entityTag) && entityManager.System<TagSystem>().HasTag(entityTag, Tag))
                    {
                        nameLocale = item.Name;
                        break;
                    }
                }
            }
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[] { ("name", Loc.TryGetString($"{Name}", out var translatedname) ? translatedname : nameLocale ?? Name) },
                Icon = Icon,
            };
        }
    }
}