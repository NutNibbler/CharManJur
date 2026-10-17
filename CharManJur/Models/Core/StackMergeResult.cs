namespace CharManJur.Models;

public enum StackMergeResult
{
    Merged,
    Swapped,
    NotSameItem,
    NotStackable,
    BothOpened,
    EmptyItem,
    TargetFull,
    Blocked
}