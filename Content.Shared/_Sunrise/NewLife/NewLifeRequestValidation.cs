namespace Content.Shared._Sunrise.NewLife;

public enum NewLifeRequestValidationResult : byte
{
    Valid,  // Ну тут все понятно, запрос валиден, можно спавнить персонажа
    MissingCharacter,
    MissingStation,
    MissingRole,
    CharacterUnavailable,
    StationUnavailable,
    RoleUnavailable,
    CooldownActive,
}

public static class NewLifeRequestValidation
{
    public static NewLifeRequestValidationResult Validate(NewLifeEuiState state, TimeSpan currentTime, int? characterId, NetEntity? stationId, string? roleProto)
    {
        if (characterId == null)
            return NewLifeRequestValidationResult.MissingCharacter;

        if (stationId == null)
            return NewLifeRequestValidationResult.MissingStation;

        if (string.IsNullOrWhiteSpace(roleProto))
            return NewLifeRequestValidationResult.MissingRole;

        if (currentTime < state.NextRespawnTime)
            return NewLifeRequestValidationResult.CooldownActive;

        var hasCharacter = false;

        foreach (var character in state.Characters)

        {
            if (character.Identifier != characterId.Value)
                continue;

            hasCharacter = true;
            break;
        }

        if (!hasCharacter || state.UsedCharactersForRespawn.Contains(characterId.Value))
            return NewLifeRequestValidationResult.CharacterUnavailable;

        if (!state.Stations.ContainsKey(stationId.Value) || !state.Jobs.TryGetValue(stationId.Value, out var roles))
            return NewLifeRequestValidationResult.StationUnavailable;

        foreach (var role in roles)
        {
            if (role.Identifier != roleProto)
                continue;

            return role.Count == 0
                ? NewLifeRequestValidationResult.RoleUnavailable
                : NewLifeRequestValidationResult.Valid;
        }

        return NewLifeRequestValidationResult.RoleUnavailable;
    }
}
