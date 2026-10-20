using System;
using System.Collections.Generic;

// A wardrobe's contents, as seen by the cabinet window (donations cabinet, client-order cabinet).
public interface ICabinetStorage : IGemContainer
{
    IReadOnlyList<GemInstance> Gems { get; }
    event Action OnCabinetChanged;
}
