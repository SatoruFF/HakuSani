namespace HakuSani;

/// <summary>Which time period an object belongs to. Haku lives in the Past, Sani in the Future.</summary>
public enum Era { Past, Future, Both }

/// <summary>
/// Both eras share one coordinate space; they are separated purely by render and physics layers.
/// That way a change in the past (moving a node) is visible "decades later" at the same spot.
/// </summary>
public static class L
{
    // Render layers (bit masks)
    public const uint VisPast = 1u << 1;
    public const uint VisFuture = 1u << 2;
    public const uint GhostOfSani = 1u << 4;   // seen by Haku's camera
    public const uint GhostOfHaku = 1u << 5;   // seen by Sani's camera
    public const uint VisShared = 1u << 6;     // unshaded effects both players see
    public const uint VisCine = 1u << 7;       // cinematic sets (prologue / finale)

    // Physics layers
    public const uint PhysPast = 1u << 0;
    public const uint PhysFuture = 1u << 1;
    public const uint PhysHaku = 1u << 2;
    public const uint PhysSani = 1u << 3;

    public static uint Vis(Era e) => e switch
    {
        Era.Past => VisPast,
        Era.Future => VisFuture,
        _ => VisShared,
    };

    public static uint Phys(Era e) => e switch
    {
        Era.Past => PhysPast,
        Era.Future => PhysFuture,
        _ => PhysPast | PhysFuture,
    };

    public static uint CatBody(Era e) => e == Era.Past ? PhysHaku : PhysSani;

    public static uint CameraMask(Era e) => e == Era.Past
        ? VisPast | GhostOfSani | VisShared
        : VisFuture | GhostOfHaku | VisShared;

    public static bool Matches(Era who, Era catEra) => who == Era.Both || who == catEra;
}
