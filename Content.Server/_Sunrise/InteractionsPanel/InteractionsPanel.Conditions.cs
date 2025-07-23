using System.Linq;
using Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    public bool CheckAllAppearConditions(InteractionPrototype interaction, EntityUid initiator, EntityUid target)
    {
        return interaction.AppearConditions.All(condition => condition.IsMet(initiator, target, EntityManager));
    }
}
