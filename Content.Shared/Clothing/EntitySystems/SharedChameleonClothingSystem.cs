using Content.Shared._Sunrise.Biocode;
using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Contraband;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly ContrabandSystem _contraband = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] private readonly BiocodeSystem _biocodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnGotEquipped(EntityUid uid, ChameleonClothingComponent component, GotEquippedEvent args)
    {
        component.User = args.Equipee;
    }

    private void OnGotUnequipped(EntityUid uid, ChameleonClothingComponent component, GotUnequippedEvent args)
    {
        component.User = null;
    }

    // Updates chameleon visuals and meta information.
    // This function is called on a server after user selected new outfit.
    // And after that on a client after state was updated.
    // This 100% makes sure that server and client have exactly same data.
    protected void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component)
    {
        if (string.IsNullOrEmpty(component.Default) ||
            !_proto.TryIndex(component.Default, out EntityPrototype? proto))
            return;

        // world sprite icon
        UpdateSprite(uid, proto);

        // copy name and description, unless its an ID card
        if (!HasComp<IdCardComponent>(uid))
        {
            var meta = MetaData(uid);
            _metaData.SetEntityName(uid, proto.Name, meta);
            _metaData.SetEntityDescription(uid, proto.Description, meta);
        }

        // item sprite logic
        if (TryComp(uid, out ItemComponent? item) &&
            proto.TryGetComponent(out ItemComponent? otherItem, _factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent("Clothing", out ClothingComponent? otherClothing))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }

        // appearance data logic
        if (TryComp(uid, out AppearanceComponent? appearance) &&
            proto.TryGetComponent("Appearance", out AppearanceComponent? appearanceOther))
        {
            _appearance.AppendData(appearanceOther, uid);
            Dirty(uid, appearance);
        }

        // properly mark contraband
        if (proto.TryGetComponent("Contraband", out ContrabandComponent? contra))
        {
            EnsureComp<ContrabandComponent>(uid, out var current);
            _contraband.CopyDetails(uid, contra, current);
        }
        else
        {
            RemComp<ContrabandComponent>(uid);
        }
            HelmetUpdate(uid, proto); // Sunrise-edit
    }
    // Sunrise-start
    // I know that this is shitcode.
    // I could subdivide UpdateVisuals into different methods, but I don't want to change the wizden code much
    public void HelmetUpdate(EntityUid uid, EntityPrototype proto)
    {
        if (!TryComp(uid, out ToggleableClothingComponent? helmet))
            return;

        if (helmet.ClothingUid == null)
            return;

        if (!proto.TryGetComponent(out ToggleableClothingComponent? protoHelmet, _factory))
            return;

        if (!_proto.TryIndex(protoHelmet.ClothingPrototype.Id, out var prototypeHelmetOther))
            return;

        if (TryComp(helmet.ClothingUid, out ClothingComponent? helmetClothing)
            && prototypeHelmetOther.TryGetComponent(out ClothingComponent? otherHelmetClothing, _factory))
        {
            _clothingSystem.CopyVisuals(helmet.ClothingUid.Value, otherHelmetClothing, helmetClothing);
        }
        if (TryComp(helmet.ClothingUid, out AppearanceComponent? helmetApperance)
            && prototypeHelmetOther.TryGetComponent(out AppearanceComponent? otherHelmetApperance, _factory))
        {
            _appearance.AppendData(otherHelmetApperance, helmet.ClothingUid.Value);
            Dirty(uid, helmetApperance);
        }
        if (TryComp(helmet.ClothingUid, out MetaDataComponent? meta))
        {
            _metaData.SetEntityName(helmet.ClothingUid.Value, prototypeHelmetOther.Name, meta);
            _metaData.SetEntityDescription(helmet.ClothingUid.Value, prototypeHelmetOther.Description, meta);
        }
        if (prototypeHelmetOther.TryGetComponent("Contraband", out ContrabandComponent? contra))
        {
            EnsureComp<ContrabandComponent>(helmet.ClothingUid.Value, out var current);
            _contraband.CopyDetails(helmet.ClothingUid.Value, contra, current);
        }
        else
        {
            RemComp<ContrabandComponent>(helmet.ClothingUid.Value);
        }

    }
    // Sunrise-end

    private void OnVerb(Entity<ChameleonClothingComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.User != args.User)
            return;

        // Can't pass args from a ref event inside of lambdas
        var user = args.User;

        // Sunrise-Start
        if (TryComp<BiocodeComponent>(ent.Owner, out var biocodedComponent))
        {
            if (!_biocodeSystem.CanUse(args.User, biocodedComponent.Factions))
                return;
        }
        // Sunrise-End

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-component-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonUiKey.Key, user)
        });
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    public bool IsValidTarget(EntityPrototype proto, SlotFlags chameleonSlot = SlotFlags.NONE, string? requiredTag = null)
    {
        // check if entity is valid
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        // check if it is marked as valid chameleon target
        if (!proto.TryGetComponent(out TagComponent? tag, _factory) || !_tag.HasTag(tag, "WhitelistChameleon"))
            return false;

        if (requiredTag != null && !_tag.HasTag(tag, requiredTag))
            return false;

        // check if it's valid clothing
        if (!proto.TryGetComponent("Clothing", out ClothingComponent? clothing))
            return false;
        if (!clothing.Slots.HasFlag(chameleonSlot))
            return false;

        return true;
    }
}
