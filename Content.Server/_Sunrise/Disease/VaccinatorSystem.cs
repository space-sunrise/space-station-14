// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Paper;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Collections.Frozen;
using Content.Shared.Paper;
using Robust.Shared.Utility;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Reagent;
using System.Linq;
using System.Text;
using Content.Shared.Power;

namespace Content.Server.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class VaccinatorSystem : SharedVaccinatorSystem
{
    /// <inheritdoc/>
    ///
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// A cache of all reactions indexed by at most ONE of their required reactants.
    /// I.e., even if a reaction has more than one reagent, it will only ever appear once in this dictionary.
    /// </summary>
    private FrozenDictionary<string, List<ReactionPrototype>> _reactionsSingle = default!;

    /// <summary>
    ///     A cache of all reactions indexed by one of their required reactants.
    /// </summary>
    private FrozenDictionary<string, List<ReactionPrototype>> _reactions = default!;



    public override void Initialize()
    {
        base.Initialize();
        InitializeReactionCache();

        SubscribeLocalEvent<VaccinatorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeNetworkEvent<PaperInputTextMessageLigyb>(OnPaper);
    }


    /// <summary>
    ///     Handles building the reaction cache.
    /// </summary>
    private void InitializeReactionCache()
    {
        // Construct single-reaction dictionary.
        var dict = new Dictionary<string, List<ReactionPrototype>>();
        foreach (var reaction in _prototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            // For this dictionary we only need to cache based on the first reagent.
            var reagent = reaction.Reactants.Keys.First();
            var list = dict.GetOrNew(reagent);
            list.Add(reaction);
        }
        _reactionsSingle = dict.ToFrozenDictionary();

        dict.Clear();
        foreach (var reaction in _prototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (var reagent in reaction.Reactants.Keys)
            {
                var list = dict.GetOrNew(reagent);
                list.Add(reaction);
            }
        }
        _reactions = dict.ToFrozenDictionary();
    }

    private void OnPaper(PaperInputTextMessageLigyb args)
    {
        EntityUid? paper = null;
        bool printeds = false;
        List<string> wasProto = new List<string>();
        foreach (var reactant in args.ReagentQuantity)
        {
            if (_prototypeManager.TryIndex(reactant.Reagent.Prototype, out ReagentPrototype? protoss))
            {
                if (protoss.Group != "Infect")
                {
                    if (paper != null)
                    {
                        EntityManager.DeleteEntity(paper);
                        return;
                    }
                }
            }

            if (wasProto.Contains(reactant.Reagent.Prototype)) { continue; } else { wasProto.Add(reactant.Reagent.Prototype); }
            if (_reactions.TryGetValue(reactant.Reagent.Prototype, out var reactantReactions))
            {
                if (printeds) continue;
                else printeds = true;
                var printed = EntityManager.SpawnEntity("ForensicReportPaper", Transform(GetEntity(args.Uid)).Coordinates);
                paper = printed;
                _metaData.SetEntityName(printed, "Технология изготовления вакцины");
                var text = new StringBuilder();
                text.AppendLine("Для изготовления вакцины, требуется:");
                text.AppendLine();
                foreach (var r in reactantReactions)
                {
                    foreach (var reactan in r.Reactants)
                    {
                        if (r.MixingCategories == null)
                        {
                            _prototypeManager.TryIndex(reactan.Key, out ReagentPrototype? proto);
                            string no = "";
                            text.AppendLine($"{proto?.LocalizedName ?? no}: {reactan.Value.Amount}u");
                        }
                    }
                }
                text.AppendLine("После чего положить полученную жидкость в вакцинатор, добавив одну каплю крови здорового человека.");
                if(TryComp<PaperComponent>(printed, out var paperComp))
                {
                    _paperSystem.SetContent((printed, paperComp), text.ToString());
                }
            }
        }
    }

    private void OnPowerChanged(Entity<VaccinatorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopMix(ent);
    }

    protected override bool HasPower(Entity<VaccinatorComponent> entity)
    {
        return this.IsPowered(entity, EntityManager);
    }
}
