using Content.Shared.EnergyDome;
using Robust.Shared.Map;

#pragma warning disable IDE0130 // Пространство имён соответствует расширяемой системе.
namespace Content.Server.EnergyDome;

public sealed partial class EnergyDomeSystem
{
    /// <summary>
    /// Пытается перенести активный купол с прежней защищаемой сущности на текущую.
    /// </summary>
    public bool TryTransferDome(Entity<EnergyDomeGeneratorComponent> generator)
    {
        if (!CanTransferDome(generator, out var previousProtectedEntity, out var protectedEntity, out var dome))
            return false;

        DoTransferDome(generator, previousProtectedEntity, protectedEntity, dome);
        return true;
    }

    /// <summary>
    /// Проверяет, можно ли перенести активный купол на текущую защищаемую сущность.
    /// </summary>
    public bool CanTransferDome(
        Entity<EnergyDomeGeneratorComponent> generator,
        out EntityUid previousProtectedEntity,
        out EntityUid protectedEntity,
        out EntityUid dome)
    {
        previousProtectedEntity = generator.Comp.DomeParentEntity ?? EntityUid.Invalid;
        protectedEntity = GetProtectedEntity(generator);
        dome = generator.Comp.SpawnedDome ?? EntityUid.Invalid;

        if (!generator.Comp.TransferDomeOnParentChange || !generator.Comp.Enabled)
            return false;

        if (previousProtectedEntity == EntityUid.Invalid || previousProtectedEntity == protectedEntity)
            return false;

        return dome != EntityUid.Invalid && Exists(dome);
    }

    private void DoTransferDome(
        Entity<EnergyDomeGeneratorComponent> generator,
        EntityUid previousProtectedEntity,
        EntityUid protectedEntity,
        EntityUid dome)
    {
        if (Exists(previousProtectedEntity) && HasComp<EnergyDomeProtectedUserComponent>(previousProtectedEntity))
            RemCompDeferred<EnergyDomeProtectedUserComponent>(previousProtectedEntity);

        var protectedComp = EnsureComp<EnergyDomeProtectedUserComponent>(protectedEntity);
        protectedComp.DomeEntity = dome;

        generator.Comp.DomeParentEntity = protectedEntity;
        _transform.SetCoordinates(dome, new EntityCoordinates(protectedEntity, default));
    }

    private bool CanPreserveDomeDuringGeneratorTransfer(Entity<EnergyDomeProtectedUserComponent> protectedEntity)
    {
        if (!TryComp<EnergyDomeGeneratorComponent>(protectedEntity, out var generator))
            return false;

        return generator.TransferDomeOnParentChange &&
               generator.Enabled &&
               protectedEntity.Comp.DomeEntity == generator.SpawnedDome;
    }
}
