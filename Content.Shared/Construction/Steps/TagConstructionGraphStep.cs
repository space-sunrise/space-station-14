using Content.Shared.Tag;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class TagConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();
            return !string.IsNullOrEmpty(_tag) && tagSystem.HasTag(uid, _tag);
        }
    }
}
