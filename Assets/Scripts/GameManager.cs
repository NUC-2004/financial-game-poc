using System;
using UnityEngine;

public partial class GameManager : MonoBehaviour
{
    [SerializeField] private EconomyManager economy;

    private const int RunLength = 10;
    private const int DailyFoodCost = 20;
    private const int DailyOtherCost = 10;
    private const int RentAmount = 500;
    private const int PayAmount = 700;

    public event Action StateChanged;

    public bool HasStarted { get; private set; }
    public bool HasEnded { get; private set; }
    public bool Completed { get; private set; }

    public int DayNumber { get; private set; }
    public int FoodDays { get; private set; }

    public DateTime CurrentDate { get; private set; }
    public DateTime NextRentDate { get; private set; }
    public DateTime Payday { get; private set; }
    public DateTime Birthday { get; private set; }

    public bool SalaryReceived { get; private set; }

    public string LastSettlement { get; private set; }
    public string EndingMessage { get; private set; }

    public int FoodCostToday =>
        FoodDays > 0 ? 0 : DailyFoodCost;

    public int LivingCostToday =>
        FoodCostToday + DailyOtherCost;

    public int RentCost => RentAmount;
    public int SalaryAmount => PayAmount;
    public int TotalDays => RunLength;

    public void StartRandomRun()
    {
        // Prevent accidentally replacing an active run.
        if (HasStarted && !HasEnded)
            return;

        int[] openingBalances = { 740, 760, 780 };
        int openingBalance =
            openingBalances[UnityEngine.Random.Range(
                0, openingBalances.Length)];

        CurrentDate = new DateTime(2026, 9, 11);
        NextRentDate = new DateTime(2026, 9, 15);
        Payday = new DateTime(2026, 9, 18);
        Birthday = new DateTime(2026, 9, 15);

        DayNumber = 1;
        FoodDays = 0;

        HasStarted = true;
        HasEnded = false;
        Completed = false;
        SalaryReceived = false;

        EndingMessage = "";
        LastSettlement =
            "Your account is ready. Check your planner before spending.";

        economy.StartNewAccount(CurrentDate, openingBalance);
        ResetEvents();

        StateChanged?.Invoke();
    }

    // Utility for future food grants; event choices use one atomic resolution below.
    public bool TryBuyFood(int price, int days)
    {
        if (!HasStarted || HasEnded || price <= 0 || days <= 0)
            return false;

        bool paid = economy.TrySpend(
            CurrentDate,
            price,
            $"Groceries: {days} days of food");

        if (!paid)
            return false;

        FoodDays += days;
        StateChanged?.Invoke();
        return true;
    }

    public void EndDay()
    {
        if (!HasStarted || HasEnded || HasPendingChoice)
            return;

        int foodCost = FoodCostToday;
        int dailyTotal = LivingCostToday;

        // Check the full daily amount before deducting anything.
        if (economy.Balance < dailyTotal)
        {
            FinishRun(
                false,
                $"You could not cover today's living costs.\n\n" +
                $"Required: ${dailyTotal}\n" +
                $"Available: ${economy.Balance}\n" +
                $"Shortfall: ${dailyTotal - economy.Balance}");

            return;
        }

        if (foodCost > 0)
        {
            economy.TrySpend(
                CurrentDate,
                foodCost,
                "Daily food");
        }
        else
        {
            FoodDays--;
        }

        economy.TrySpend(
            CurrentDate,
            DailyOtherCost,
            "Other daily essentials");

        LastSettlement =
            $"Living costs paid: ${dailyTotal}.";

        // Rent is checked after today's living costs.
        if (CurrentDate.Date == NextRentDate.Date)
        {
            bool rentPaid = economy.TrySpend(
                CurrentDate,
                RentAmount,
                "Weekly rent");

            if (!rentPaid)
            {
                FinishRun(
                    false,
                    $"Rent was due today, but you could not pay it.\n\n" +
                    $"Required: ${RentAmount}\n" +
                    $"Available after living costs: ${economy.Balance}\n" +
                    $"Shortfall: ${RentAmount - economy.Balance}");

                return;
            }

            LastSettlement +=
                $"\nWeekly rent paid: ${RentAmount}.";

            NextRentDate = NextRentDate.AddDays(7);
        }

        if (DayNumber >= RunLength)
        {
            FinishRun(
                true,
                $"You completed all {RunLength} days.\n\n" +
                $"Closing balance: ${economy.Balance}\n" +
                "Review your ledger and upcoming commitments.");

            return;
        }

        DayNumber++;
        CurrentDate = CurrentDate.AddDays(1);

        // Confirmed income arrives at the start of the day.
        if (!SalaryReceived &&
            CurrentDate.Date >= Payday.Date)
        {
            economy.AddIncome(
                CurrentDate,
                PayAmount,
                "Part-time wages");

            SalaryReceived = true;

            LastSettlement +=
                $"\nNew day: wages of ${PayAmount} have arrived.";
        }

        SettleEventPayments();
        PrepareDayEvent();
        StateChanged?.Invoke();
    }

    private void FinishRun(bool completed, string message)
    {
        HasEnded = true;
        Completed = completed;
        EndingMessage = message;

        StateChanged?.Invoke();
    }
}