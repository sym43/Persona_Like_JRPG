/// <summary>
/// 전투 행동 대상 종류.<br/>
/// OneEnemy(적 1명), AllEnemies(적 전체), OneAlly(아군 1명),<br/>
/// AllAllies(아군 전체), Self(자기 자신), OneDeadAlly(전투 불능 아군 1명).
/// </summary>
public enum BattleTargetType
{
    OneEnemy = 0,
    AllEnemies = 1,
    OneAlly = 2,
    AllAllies = 3,
    Self = 4,
    OneDeadAlly = 5
}
