using Content.Server.Nuke;
using Content.Shared._Sunrise.EncodedAirlock;
using Content.Shared.Audio;
using Content.Shared.Nuke;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;


namespace Content.Server._Sunrise.EncodedAirlock;

public sealed class CodeConsoleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CodeConsoleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<NukeComponent, NukeArmedMessage>(OnArmButtonPressed);
        SubscribeLocalEvent<CodeConsoleComponent, CodeConsoleKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<CodeConsoleComponent, CodeConsoleKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<CodeConsoleComponent, CodeConsoleKeypadEnterMessage>(OnEnterButtonPressed);
    }

    private void OnMapInit(Entity<CodeConsoleComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.Code))
            ent.Comp.Code = GetRandomCode(ent.Comp.CodeLength);
    }

    private void OnKeypadButtonPressed(Entity<CodeConsoleComponent> ent, ref CodeConsoleKeypadMessage args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlayKeypadSound(ent, args.Value);

        if (!component.IsLocked)
            return;

        if (component.EnteredCode.Length >= component.CodeLength)
            return;

        component.EnteredCode += args.Value.ToString();
        UpdateUserInterface(ent);
    }

    private void OnClearButtonPressed(Entity<CodeConsoleComponent> ent, ref CodeConsoleKeypadClearMessage _)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        _audio.PlayPvs(component.KeypadPressSound, uid);

        if (!component.IsLocked)
            return;

        component.EnteredCode = "";
        UpdateUserInterface(ent);
    }

    private void OnEnterButtonPressed(Entity<CodeConsoleComponent> ent, ref CodeConsoleKeypadEnterMessage _)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (!component.IsLocked)
            return;

        UpdateStatus(ent);
        UpdateUserInterface(ent);
    }

    private void UpdateStatus(Entity<CodeConsoleComponent> ent)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (!Resolve(uid, ref component))
            return;

        if (component.IsLocked)
        {
            if (component.EnteredCode == component.Code)
            {
                component.IsLocked = false;
                _audio.PlayPvs(component.AccessGrantedSound, uid);
            }
            else
            {
                component.EnteredCode = "";
                _audio.PlayPvs(component.AccessDeniedSound, uid);
            }
        }
    }

    private void PlayKeypadSound(Entity<CodeConsoleComponent> ent, int number)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        // This is a C mixolydian blues scale.
        // 1 2 3    C D Eb
        // 4 5 6    E F F#
        // 7 8 9    G A Bb
        var semitoneShift = number switch
        {
            1 => 0,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 7,
            8 => 9,
            9 => 10,
            0 => 8,
            _ => 0
        };

        var opts = component.KeypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-5f);
        _audio.PlayPvs(component.KeypadPressSound, uid, opts);
    }

    private void UpdateUserInterface(Entity<CodeConsoleComponent> ent)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (!Resolve(uid, ref component))
            return;

        if (!_ui.HasUi(uid, CodeConsoleUiKey.Key))
            return;

        var state = new CodeConsoleUiState
        {
            IsLocked = component.IsLocked,
            EnteredCodeLength = component.EnteredCode.Length,
            MaxCodeLength = component.CodeLength
        };

        _ui.SetUiState(uid, CodeConsoleUiKey.Key, state);
    }

    private string GetRandomCode(int codeLength)
    {
        var symbols = "1234567890".ToCharArray();
        var code = new char[codeLength];

        for (int i = 0; i < codeLength; i++)
        {
            code[i] = _random.Pick(symbols);
        }

        return new string(code);
    }
}
