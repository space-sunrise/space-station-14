// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PlaytimeTop;

/// <summary>
/// Запись топа игроков по онлайну: логин и суммарное время.
/// </summary>
[Serializable, NetSerializable]
public sealed record PlaytimeTopEntry(string Username, TimeSpan TotalTime);
