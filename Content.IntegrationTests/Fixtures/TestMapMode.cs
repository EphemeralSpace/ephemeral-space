namespace Content.IntegrationTests.Fixtures;

/// <summary>
///     Possible configurations for <see cref="GameTest.TestMapSetting"/>.
/// </summary>
/// <seealso cref="TestMapMode.None"/>
/// <seealso cref="TestMapMode.Basic"/>
/// <seealso cref="TestMapMode.Arena"/>
/// <seealso cref="GameTest"/>
public enum TestMapMode
{
    // REMARK: IF you add new modes suitable for TestPlayer, make sure to add them to SitAroundInnocently.

    /// <summary>
    ///     Indicates no testmap should be loaded.
    /// </summary>
    None,
    /// <summary>
    ///     Indicates a single tile, empty map should be loaded.
    ///     Atmospherics and gravity are not configured.
    /// </summary>
    Basic,
    /// <summary>
    ///     Indicates a larger 9x9 "arena" map should be created,
    ///     with atmos and gravity set up. This is useful alongside <see cref="TestPlayer"/>
    ///     for tests that need to puppeteer a player.
    /// </summary>
    Arena,
}
