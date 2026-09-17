using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 피해 속성에 대한 상성.<br/>
/// Normal(보통), Weak(약점), Resist(내성).<br/>
/// Immune(무효), Reflect(반사), Drain(흡수).
/// </summary>
public enum ResistanceType
{
    Normal = 0,
    Weak = 1,
    Resist = 2,
    Immune = 3,
    Reflect = 4,
    Drain = 5
}
