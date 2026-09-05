namespace Content.Shared._Sunrise.Records;

// Sunrise added — единая точка правды для текущего игрового года досье, чтобы дата рождения,
// дата диплома и дата присвоения звания никогда не расходились друг с другом (не превышали
// текущий год и не оказывались раньше рождения персонажа)
public static class RecordDateConventions
{
    public const int CurrentYear = 3026;
}
