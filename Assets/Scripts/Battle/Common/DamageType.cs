using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 공격·스킬의 피해 속성.<br/>
/// 물리: Slash(절단), Strike(타격), Pierce(관통).<br/>
/// 감정: Joy(희열), Anger(분노), Despair(절망), Fear(공포), Love(사랑), Hatred(증오), Desire(욕망).
/// </summary>
public enum DamageType
{
    Slash = 0,
    Strike = 1,
    Pierce = 2,
    Joy = 3,
    Anger = 4,
    Despair = 5,
    Fear = 6,
    Love = 7,
    Hatred = 8,
    Desire = 9
}
