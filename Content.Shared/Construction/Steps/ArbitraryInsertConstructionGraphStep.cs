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
                        //Sunrise-start
                        if(item.Name == "левая рука киборга-официанта")
                        {
                            nameLocale = "левая рука киборга";
                        }
                        else if(item.Name == "голова киборга-уборщика")
                        {
                            nameLocale = "голова киборга";
                        }
                        else
                        {
                            nameLocale = item.Name;
                        }
                        //Sunrise-End
                        break;
                    }
                }
            }

            if (nameLocale == null)
            {
                var formattedName = Name.Replace(" ", "-").ToLower();
                nameLocale = Loc.GetString($"material-{formattedName}-name");
            }

            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-arbitrary-step",
                Arguments = new (string, object)[] { ("name", nameLocale ?? Name) },
                Icon = Icon,
            };
        }
    }
}