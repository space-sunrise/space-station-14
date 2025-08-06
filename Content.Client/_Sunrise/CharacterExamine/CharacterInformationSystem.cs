using System.Linq;
using Content.Client.Examine;
using Content.Client.Inventory;
using Content.Shared.Access.Components;
using Content.Shared.CCVar;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Content.Shared.White.CharacterExamine;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.White.CharacterExamine;


public sealed class CharacterInformationSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly ClientInventorySystem _inventory = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private CharacterInformationWindow? _window;


    public override void Initialize()
    {
        base.Initialize();

        _window = new CharacterInformationWindow();

        SubscribeLocalEvent<CharacterInformationComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }


    private void OnGetExamineVerbs(EntityUid uid, CharacterInformationComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                // TODO: Better name?
                Do(args.Target);
            },
            Text = Loc.GetString("character-information-verb-text"),
            Message = Loc.GetString("character-information-verb-message"),
            Category = VerbCategory.Examine,
            Disabled = !_examine.IsInDetailsRange(args.User, uid),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
            ClientExclusive = true,
        };

        args.Verbs.Add(verb);
    }


    private void Do(EntityUid uid)
    {
        if (_window == null)
            return;

        string? name = null;
        string? job = null;
        string? flavorText = null;

        // Get ID from inventory, get name and job from ID
        if (_inventory.TryGetSlotEntity(uid, "id", out var idUid))
        {
            var id = GetId(idUid);
            if (id is not null)
            {
                var info = GetNameAndJob(id);

                name = info.Item1;
                job = info.Item2;
            }
        }

        // Fancy job title
        if (!string.IsNullOrEmpty(job))
        {
            var test = job.Replace(" ", "");
            // Command will be last in the list
            // TODO: Make this not revolve around this fact ^
            var departments = _prototype.EnumeratePrototypes<DepartmentPrototype>().OrderBy(d => d.ID).Reverse();
            var department = departments.FirstOrDefault(d => d.Roles.Contains(test));

            if (department is not null)
            {
                // Department (ex: Command or Security)
                var dept = string.Join(" ", Loc.GetString($"department-{department.ID}").Split(' ').Select(s => s[0].ToString().ToUpper() + s[1..].ToLower()));
                // Redo the job title with the department color and department (ex: Captain (Command) or Security Officer (Security))
                job = $"[color={department.Color.ToHex()}]{job} ({dept})[/color]";
            }
        }

        // Get and set flavor text
        if (!_config.GetCVar(CCVars.FlavorText))
        {
            flavorText = Loc.GetString("character-information-ui-flavor-text-disabled");
        }
        else if (_entity.TryGetComponent<DetailExaminableComponent>(uid, out var detail))
        {
            flavorText = detail.Content;
        }

        _window.OpenCentered();
        _window.UpdateUi(uid, name, job, flavorText);
        _examine.CloseTooltip();
    }

    /// <summary>
    ///     Gets the ID card component from either a PDA or an ID card if the entity has one
    /// </summary>
    /// <param name="idUid">Entity to check</param>
    /// <returns>ID card component if they have one on the entity</returns>
    private IdCardComponent? GetId(EntityUid? idUid)
    {
        // PDA
        if (_entity.TryGetComponent(idUid, out PDAComponent? pda) && pda.ContainedID is not null)
            return pda.ContainedID;
        // ID Card
        if (_entity.TryGetComponent(idUid, out IdCardComponent? id))
            return id;

        return null;
    }

    /// <summary>
    ///     Gets the name and job title from an ID card component
    /// </summary>
    /// <param name="id">The ID card to retrieve the information from</param>
    /// <returns>Name, Job Title</returns>
    private static (string, string) GetNameAndJob(IdCardComponent id)
    {
        var name = id.FullName;
        if (string.IsNullOrEmpty(name))
            name = "Unknown";

        var jobTitle = id.JobTitle;
        if (string.IsNullOrEmpty(jobTitle))
            jobTitle = "Unknown";
        jobTitle = string.Join(" ", jobTitle.Split(' ').Select(s => s[0].ToString().ToUpper() + s[1..].ToLower()));

        return (name, jobTitle);
    }
}
