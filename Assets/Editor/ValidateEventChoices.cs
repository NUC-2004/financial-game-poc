using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ValidateEventChoices
{
    private static void Check(bool valid, string message)
    { if (!valid) throw new Exception("EVENT VALIDATION: " + message); }

    [MenuItem("Tools/Financial Game/Validate Event Choices")]
    public static void Run()
    {
        Check(EditorApplication.isPlaying, "Enter Play mode first.");
        // All 243 planned choice sequences at each of the three opening balances.
        int[] openings = { 740, 760, 780 };
        int[] expectedWins = { 150, 165, 192 };
        int[] expectedRentFailures = { 54, 27, 0 };
        int totalWins = 0;
        var randomState = UnityEngine.Random.state;
        try
        {
            for (int tier = 0; tier < openings.Length; tier++)
            {
            int wins = 0, rentFailures = 0, livingFailures = 0;
            for (int path = 0; path < 243; path++)
            {
                var holder = new GameObject("Event validation (temporary)") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var economy = holder.AddComponent<EconomyManager>();
                    var game = holder.AddComponent<GameManager>();
                    var fields = new SerializedObject(game);
                    fields.FindProperty("economy").objectReferenceValue = economy; fields.ApplyModifiedProperties();
                    game.StartRandomRun();
                    Check(openings.Contains(economy.Balance), "Random opening belongs to tuned pool.");
                    economy.StartNewAccount(game.CurrentDate, openings[tier]);
                    int digits = path;
                    for (int day = 1; day <= 10 && !game.HasEnded; day++)
                    {
                        Check(game.DayNumber == day, "Day progression.");
                        if (game.HasPendingChoice)
                        {
                            int before = economy.Balance, entries = economy.Transactions.Count;
                            game.EndDay();
                            Check(game.DayNumber == day && economy.Balance == before, "Unanswered event blocks End Day.");
                            string id = game.CurrentEvent.Id;
                            Check(!game.TryChooseEvent("stale-event", 0) && !game.TryChooseEvent(id, -1) && !game.TryChooseEvent(id, 3), "Reject invalid requests.");
                            int optionIndex = digits % 3; digits /= 3;
                            var option = game.CurrentEvent.Options[optionIndex];
                            int foodBefore = game.FoodDays;
                            Check(game.TryChooseEvent(id, optionIndex), "Affordable option resolves.");
                            Check(economy.Balance == before - option.Cost, "No early delayed income.");
                            Check(economy.Transactions.Count == entries + (option.Cost > 0 ? 1 : 0), "Record actual cash only.");
                            Check(game.FoodDays == foodBefore + option.Food, "Food is applied exactly once.");
                            Check(!game.TryChooseEvent(id, optionIndex), "Duplicate choice rejected.");
                            Check(economy.Balance == before - option.Cost && game.FoodDays == foodBefore + option.Food, "Duplicate has no effect.");
                        }
                        int cash = economy.Balance, food = game.FoodDays;
                        int living = game.LivingCostToday;
                        bool rent = day == 5;
                        int arriving = game.PendingPayments.Where(p => p.Date.Date <= game.CurrentDate.AddDays(1).Date).Sum(p => p.Amount);
                        game.EndDay();
                        bool livingFailure = cash < living;
                        bool rentFailure = !livingFailure && rent && cash - living < 500;
                        if (livingFailure)
                        {
                            Check(game.HasEnded && !game.Completed && economy.Balance == cash && game.FoodDays == food,
                                "Living-cost failure does not partly debit the account.");
                            Check(game.EndingMessage.Contains("living costs"), "Failure explains living costs.");
                            livingFailures++;
                        }
                        else if (rentFailure)
                        {
                            Check(game.HasEnded && !game.Completed && economy.Balance == cash - living,
                                "Rent failure debits living costs only.");
                            Check(game.EndingMessage.Contains("Rent was due"), "Failure explains rent.");
                            rentFailures++;
                        }
                        else
                        {
                            int expected = cash - living - (rent ? 500 : 0) + (day == 7 ? 700 : 0) + arriving;
                            Check(economy.Balance == expected, "Living/rent/payday and delayed payment timing.");
                        }
                        if (!livingFailure)
                            Check(game.FoodDays == Math.Max(0, food - 1), "One stored food day used per night.");
                        Check(economy.Transactions.Sum(t => t.Amount) == economy.Balance, "Ledger reconciles.");
                    }
                    Check(game.HasEnded, "Every route ends or completes.");
                    if (game.Completed)
                    {
                        wins++;
                        Check(game.Decisions.Count == 5 && game.PendingPayments.Count == 0, "Full run and all decisions.");
                    }
                    game.StartRandomRun();
                    Check(game.Decisions.Count == 0 && game.PendingPayments.Count == 0 && game.FoodDays == 0 && game.HasPendingChoice, "Restart resets event state.");
                    economy.TrySpend(game.CurrentDate, economy.Balance, "Validation: empty account");
                    Check(!game.CanChooseEvent("groceries", 0) && !game.TryChooseEvent("groceries", 0), "Insufficient money rejected.");
                    Check(game.HasPendingChoice && game.Decisions.Count == 0, "Rejected choice stays pending.");
                    Check(game.TryChooseEvent("groceries", 2), "Free response stays accessible.");
                    game.EndDay();
                    Check(game.HasEnded && !game.Completed, "Cannot pay living costs.");
                    Check(!game.TryChooseEvent("groceries", 2), "No choices after ending.");
                }
                finally { UnityEngine.Object.DestroyImmediate(holder); }
            }
            Check(wins == expectedWins[tier] && rentFailures == expectedRentFailures[tier], "Expected balance distribution.");
            Check(wins + rentFailures + livingFailures == 243, "All planned sequences accounted for.");
            totalWins += wins;
            Debug.Log($"BALANCE_TIER: opening={openings[tier]}, completed={wins}/243, rent failures={rentFailures}, living failures={livingFailures}");
            }
            Check(totalWins == 507, "507 of 729 planned routes finish.");
        }
        finally { UnityEngine.Random.state = randomState; }

        // Exercise the real scene's persistent UI references and runtime button listeners.
        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        var liveGame = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var liveMoney = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
        Check(liveGame.DayNumber == 1 && liveGame.HasPendingChoice, "Run UI check from a fresh Play session.");
        ui.OpenJournal(); Canvas.ForceUpdateCanvases();
        var page = ui.transform.Find("JournalPanel/JournalWindow");
        var end = page.Find("EndDayButton").GetComponent<Button>();
        Check(!end.interactable, "End Day disabled before choice.");
        int opening = liveMoney.Balance;
        CheckFits(page);
        page.Find("ChoiceButton1").GetComponent<Button>().onClick.Invoke();
        Check(liveMoney.Balance == opening - 80 && liveGame.FoodDays == 5 && end.interactable, "UI purchase and food.");
        Check(!page.Find("ChoiceButton1").gameObject.activeSelf, "Choice disappears after resolution.");
        CheckFits(page);
        ui.CloseJournal(); ui.OpenJournal();
        Check(!liveGame.HasPendingChoice && liveMoney.Balance == opening - 80, "Reopening preserves decision.");
        for (int day = 1; day <= 10; day++)
        {
            ui.OpenJournal();
            if (liveGame.HasPendingChoice)
            {
                CheckFits(page);
                int choice = day == 3 || day == 5 ? 2 : 1;
                page.Find("ChoiceButton" + choice).GetComponent<Button>().onClick.Invoke();
                CheckFits(page);
            }
            end.onClick.Invoke();
        }
        Check(liveGame.Completed && liveMoney.Balance == opening + 50, "Golden UI path closes at opening + $50.");
        ui.OpenLedger(); Canvas.ForceUpdateCanvases();
        var history = ui.transform.Find("LedgerPanel/BookWindow/TransactionScrollView/Viewport/Content/TransactionsText").GetComponent<TMP_Text>();
        Check(history.text.Contains("DECISION HISTORY") && history.text.Contains("Textbook deposit refund") && history.text.Contains("Extra cafe shift pay"), "Review includes choices and late income.");
        ui.StartRandomRun(); ui.OpenJournal();
        Debug.Log("EVENT_VALIDATION_PASSED: 729 opening/choice combinations, 507 completed (69.5%); affordability, duplicate/stale requests, food charges, deferred payments, restart, UI buttons, text bounds, and ledger. Fresh day-one journal restored.");
    }

    private static void CheckFits(Transform page)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var label in page.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Check(label.preferredHeight <= label.rectTransform.rect.height + 2,
                label.name + " overflows its height: " + label.preferredHeight + " / " + label.rectTransform.rect.height);
        }
    }
}
