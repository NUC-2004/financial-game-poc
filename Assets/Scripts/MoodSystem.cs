using UnityEngine;

[DisallowMultipleComponent]
public sealed class MoodSystem : MonoBehaviour
{
    [Header("Authored game values, not a psychological model")]
    [Range(1, 100)] public int startingMood = 65;
    [Range(1, 99)] public int warningLevel = 25;
    [Min(0)] public int repeatWorkPenalty = 6;
    [Min(1)] public int repeatWorkWindow = 4;
    public int Value { get; private set; }
    public int Lowest { get; private set; }
    public int LastWorkDay { get; private set; } = -10000;
    public bool IsLow => Value <= warningLevel;
    public void ResetRun()
    {
        Value = Mathf.Clamp(startingMood, 1, 100);
        Lowest = Value;
        LastWorkDay = -10000;
    }
    public int Preview(int baseChange, bool isWork, int day)
    {
        int penalty = isWork && day - LastWorkDay <= repeatWorkWindow ? repeatWorkPenalty : 0;
        return Mathf.Clamp(Value + baseChange - penalty, 0, 100) - Value;
    }
    public int Apply(int baseChange, bool isWork, int day)
    {
        int change = Preview(baseChange, isWork, day);
        Value += change;
        Lowest = Mathf.Min(Lowest, Value);
        if (isWork) LastWorkDay = day;
        else if (baseChange > 0) LastWorkDay = -10000; // Recovery breaks the extra-work streak.
        return change;
    }
}
