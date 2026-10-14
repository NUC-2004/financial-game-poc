using UnityEngine;

[DisallowMultipleComponent]
public sealed class FinanceRules : MonoBehaviour
{
    [Header("Run")]
    [Min(1)] public int runDays = 30;
    public int[] openingBalances = { 740, 780, 820 };
    [Header("Daily settlement: food, essentials, then rent")]
    [Min(1)] public int dailyFood = 20;
    [Min(1)] public int dailyEssentials = 10;
    [Header("Weekly rent")]
    [Min(1)] public int rent = 500;
    [Min(1)] public int firstRentDay = 5;
    [Min(1)] public int rentInterval = 7;
    [Header("Regular wages arrive at the START of the day")]
    [Min(1)] public int wages = 540;
    [Min(2)] public int firstPayDay = 8;
    [Min(1)] public int payInterval = 7;

    public bool IsValid => runDays > 0 && openingBalances != null && openingBalances.Length > 0 &&
        System.Array.TrueForAll(openingBalances, n => n >= 0) && dailyFood > 0 && dailyEssentials > 0 &&
        rent > 0 && firstRentDay > 0 && rentInterval > 0 && wages > 0 && firstPayDay >= 2 && payInterval > 0;
}
