using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Vampire.Components;
using Robust.Shared.Console;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public void InitializeCommand()
    {
        _consoleHost.RegisterCommand("addbloodessence",
            "Adds blood essence to vampire. Debug command.",
            "addbloodessence <uid> <amount>",
            AddBloodEssenceCommand,
            AddBloodEssenceCommandCompletions);
    }

    [AdminCommand(AdminFlags.VarEdit)]
    private void AddBloodEssenceCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid) ||
            !float.TryParse(args[1], out var quantity))
        {
            return;
        }

        if (!TryComp<VampireComponent>(uid, out var vampireComponent))
            return;

        AddBloodEssence((uid.Value, vampireComponent), quantity);
    }

    private CompletionResult AddBloodEssenceCommandCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var query = EntityQueryEnumerator<VampireComponent>();
            var allVampires = new List<string>();
            while (query.MoveNext(out var vampireUid, out _))
            {
                allVampires.Add(vampireUid.ToString());
            }

            return CompletionResult.FromHintOptions(allVampires, "<uid>");
        }

        return CompletionResult.Empty;
    }
}
