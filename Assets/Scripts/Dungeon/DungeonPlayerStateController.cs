using UnityEngine;

public enum DungeonPlayerState
{
    Free,
    Attacking,
    Interacting,
    EnteringBattle
}

[DisallowMultipleComponent]
public sealed class DungeonPlayerStateController : MonoBehaviour
{
    public DungeonPlayerState CurrentState { get; private set; } = DungeonPlayerState.Free;
    public bool CanMove => CurrentState == DungeonPlayerState.Free;

    public bool TryEnter(DungeonPlayerState nextState)
    {
        if (nextState == DungeonPlayerState.Free || CurrentState != DungeonPlayerState.Free)
        {
            return false;
        }

        CurrentState = nextState;
        return true;
    }

    public bool TryReturnToFree(DungeonPlayerState expectedState)
    {
        if (CurrentState != expectedState)
        {
            return false;
        }

        CurrentState = DungeonPlayerState.Free;
        return true;
    }
}
