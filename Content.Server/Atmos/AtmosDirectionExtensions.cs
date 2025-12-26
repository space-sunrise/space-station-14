using Content.Shared.Atmos;

namespace Content.Server.Atmos;

public static class AtmosDirectionExtensions
{
    public static Vector2i ToVec(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.North => new Vector2i(0, 1),
            AtmosDirection.South => new Vector2i(0, -1),
            AtmosDirection.East => new Vector2i(1, 0),
            AtmosDirection.West => new Vector2i(-1, 0),
            _ => Vector2i.Zero
        };
    }

    public static AtmosDirection GetOpposite(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.North => AtmosDirection.South,
            AtmosDirection.South => AtmosDirection.North,
            AtmosDirection.East => AtmosDirection.West,
            AtmosDirection.West => AtmosDirection.East,
            _ => AtmosDirection.Invalid
        };
    }

    public static int ToIndex(this AtmosDirection direction)
    {
        return direction switch
        {
            AtmosDirection.North => 0,
            AtmosDirection.South => 1,
            AtmosDirection.East => 2,
            AtmosDirection.West => 3,
            _ => -1
        };
    }

    public static int ToOppositeIndex(this int index)
    {
        return index switch
        {
            0 => 1,
            1 => 0,
            2 => 3,
            3 => 2,
            _ => -1
        };
    }
}
