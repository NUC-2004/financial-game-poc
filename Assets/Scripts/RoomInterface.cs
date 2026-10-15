using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoomInterface : MonoBehaviour
{
    public GameManager game;
    public EconomyManager economy;
    public GameUI gameUI;
    public GameObject calendarPanel;
    public GameObject infoPanel;
    public TMP_Text calendarTitle;
    public TMP_Text calendarDetail;
    public TMP_Text infoTitle;
    public TMP_Text infoBody;
    public ScrollRect infoScroll;
    public Button[] calendarDays;
    public TMP_Text[] calendarLabels;
    public TMP_Text nextBillText;
    public TMP_Text fridgeLabel;
    public TMP_Text journalLabel;
    public Button tipButton;
    public Button returnJournalButton;
    public TMP_Text rentPlanText;
    public Button reserveRentButton, releaseRentButton;
    public Button groceryShopButton, smallGroceryButton, bulkGroceryButton;
    public Button shopTrackerButton;
    private DateTime shownMonth;
    private DateTime selectedDate;
    private string infoMode;
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-AU");
    private void OnEnable() { if (game != null) game.StateChanged += Refresh; }
    private void OnDisable() { if (game != null) game.StateChanged -= Refresh; }
    private void Start() { ApplyPresentation(); ClosePanels(); Refresh(); }
    public void ApplyPresentation()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.pixelPerfect = true;
        var root = transform.Find("GamePanel/IllustratedRoom") as RectTransform;
        if (root == null) return;
        // Fill the available window. Hotspots share the image's normalized coordinates.
        var fit = root.GetComponent<AspectRatioFitter>();
        if (fit != null) { fit.aspectMode = AspectRatioFitter.AspectMode.None; fit.enabled = false; }
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.pivot = new Vector2(.5f, .5f); root.offsetMin = root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            float size = label.name == "BalanceText" ? 34 : label.name == "DateText" || label.name == "DataText" ? 30
                : label.name == "MoodText" ? 30 : label.name == "NextBill" ? 27 : label.name == "RoomHint" ? 22 : 26;
            label.fontSize = size; label.enableAutoSizing = true;
            label.fontSizeMin = label.name == "RoomHint" ? 18 : 22; label.fontSizeMax = size;
            label.fontStyle = label.name == "Label" ? FontStyles.Bold : FontStyles.Normal;
        }
    }
    public void ClosePanels()
    {
        calendarPanel.SetActive(false); infoPanel.SetActive(false);
    }
    private void HideBooks()
    {
        gameUI.CloseJournal(); gameUI.CloseLedger(); ClosePanels();
    }
    public void OpenFridge()
    {
        if (!game.HasStarted) return;
        HideBooks(); infoMode = "fridge"; infoPanel.SetActive(true); returnJournalButton.gameObject.SetActive(false);
        RefreshFridge(); ResetInfoScroll();
    }
    private void RefreshFridge()
    {
        infoTitle.text = "FRIDGE CHECK";
        infoBody.text = $"<size=48><b>{game.FoodDays} food day(s)</b></size>\n\n" +
            (game.AteTodaysMeal ? "Today's meal is already eaten. No second portion tonight." : game.FoodDays > 0
                ? "One stored portion will cover tonight's food." : $"No stored meals. Tonight's food costs ${game.FoodCostToday}.") +
            $"\n\nOther essentials: ${game.Rules.dailyEssentials}/day.\nTonight's total living costs: ${game.LivingCostToday}.\n\n" +
            "Groceries are purchased through journal events. Stored meals stack and do not expire.\n\n" + game.FoodDiscountSummary;
        RefreshInfoActions();
    }
    private void RefreshInfoActions()
    {
        if (groceryShopButton != null) groceryShopButton.gameObject.SetActive(false);
        if (shopTrackerButton != null) shopTrackerButton.gameObject.SetActive(false);
        if (smallGroceryButton != null) smallGroceryButton.gameObject.SetActive(false);
        if (bulkGroceryButton != null) bulkGroceryButton.gameObject.SetActive(false);
        infoScroll.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 550);
        returnJournalButton.gameObject.SetActive(infoMode != "fridge");
    }
    public void ReserveRent() { game.ReserveForRent(); RefreshRentPlan(); }
    public void ReleaseRent() { game.ReleaseRentReserve(); RefreshRentPlan(); }
    public void RefreshRentPlan()
    {
        if (rentPlanText == null || !game.HasStarted) return;
        rentPlanText.text = $"TOTAL BALANCE: ${economy.Balance}\nRENT PLAN: ${game.RentReserve} / ${game.RentCost}\nUNALLOCATED: ${game.UnallocatedCash}\nIncluded in total. Living costs may use it.";
        int addition = Math.Max(0, Math.Min(game.RentCost, economy.Balance) - game.RentReserve);
        reserveRentButton.interactable = !game.HasEnded && addition > 0;
        reserveRentButton.GetComponentInChildren<TMP_Text>().text = addition > 0 ? $"Reserve ${addition}" : "Rent reserved";
        releaseRentButton.interactable = !game.HasEnded && game.RentReserve > 0;
    }
    public void OpenTip()
    {
        if (!game.HasStarted || game.HasEnded || game.CurrentEvent == null) return;
        HideBooks(); infoMode = "tip"; infoPanel.SetActive(true); returnJournalButton.gameObject.SetActive(true);
        string key = game.CurrentEvent.TipKey;
        infoTitle.text = StrategySystem.TipTitle(key);
        infoBody.text = StrategySystem.Tip(key);
        RefreshInfoActions();
        ResetInfoScroll();
    }
    public void ReturnToJournal() { ClosePanels(); gameUI.OpenJournal(); }
    public void OpenCalendar()
    {
        if (!game.HasStarted) return;
        HideBooks(); selectedDate = game.CurrentDate;
        shownMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
        calendarPanel.SetActive(true); RefreshCalendar();
    }
    public void PreviousMonth() { shownMonth = shownMonth.AddMonths(-1); RefreshCalendar(); }
    public void NextMonth() { shownMonth = shownMonth.AddMonths(1); RefreshCalendar(); }
    private void ResetInfoScroll()
    {
        Canvas.ForceUpdateCanvases(); infoScroll.verticalNormalizedPosition = 1; infoScroll.StopMovement();
    }
    private void Refresh()
    {
        if (game == null || !game.HasStarted) return;
        nextBillText.text = $"RENT ${game.RentCost}  /  {GameManager.ShortDate(game.NextRentDate)}" +
            (game.NextRentDate.Date == game.CurrentDate.Date ? "  TODAY" : "");
        fridgeLabel.text = $"FRIDGE  /  {game.FoodDays} DAYS";
        journalLabel.text = game.HasEnded ? "REVIEW RUN" : game.HasPendingChoice ? "JOURNAL  /  NEW" : "DAILY JOURNAL";
        if (calendarPanel.activeSelf) RefreshCalendar();
        if (infoPanel.activeSelf && infoMode == "fridge") RefreshFridge();

        RefreshRentPlan();
        tipButton.gameObject.SetActive(!game.HasEnded && game.CurrentEvent != null);
    }
    public List<string> Entries(DateTime date, bool compact = false)
    {
        var list = new List<string>();
        var start = game.CurrentDate.AddDays(1 - game.DayNumber).Date;
        int day = (date.Date - start).Days + 1;
        if (day < 1) return list;
        if (day >= game.Rules.firstRentDay && (day - game.Rules.firstRentDay) % game.Rules.rentInterval == 0)
            list.Add(compact ? "RENT" : $"Rent: -${game.RentCost} after living costs");
        if (day >= game.Rules.firstPayDay && (day - game.Rules.firstPayDay) % game.Rules.payInterval == 0)
            list.Add(compact ? "ALLOWANCE" : $"Family allowance: +${game.SalaryAmount} in the morning");
        if (day <= game.TotalDays)
        {
            // An internal event date is not an appointment known to the player.
            if (game.Schedule.events.Any(e => e.id == "birthday" && e.day == day))
                list.Add(compact ? "BIRTHDAY" : "Alex's birthday");
            if (day == 16 && game.Strategies.ReminderSet) list.Add(compact ? "REMINDER" : "Reminder: review your $40 emergency-buffer goal");
            if (day == 10 && game.Strategies.WaitingForPurchase) list.Add(compact ? "WISHLIST" : "Revisit your headphone wishlist");
        }
        foreach (var p in game.PendingPayments.Where(p => p.Date.Date == date.Date))
            list.Add(compact ? "+$" + p.Amount : $"Confirmed income +${p.Amount}: {p.Description}");
        return list;
    }
    public void RefreshCalendar()
    {
        calendarTitle.text = shownMonth.ToString("MMMM yyyy", English).ToUpperInvariant();
        int offset = ((int)shownMonth.DayOfWeek + 6) % 7;
        for (int i = 0; i < calendarDays.Length; i++)
        {
            var date = shownMonth.AddDays(i - offset);
            var button = calendarDays[i];
            button.onClick.RemoveAllListeners();
            var entries = Entries(date, true);
            calendarLabels[i].text = date.Day + "\n<size=16>" + string.Join("\n", entries.Take(3)) + (entries.Count > 3 ? "\n+MORE" : "") + "</size>";
            bool today = date.Date == game.CurrentDate.Date;
            bool selected = date.Date == selectedDate.Date;
            button.GetComponent<Image>().color = selected ? new Color32(211, 174, 96, 255) : today
                ? new Color32(180, 203, 177, 255) : date.Month == shownMonth.Month ? new Color32(235, 225, 201, 255) : new Color32(210, 205, 192, 255);
            button.onClick.AddListener(() => { selectedDate = date; RefreshCalendar(); });
        }
        string detail = selectedDate.ToString("ddd, dd MMM yyyy", English);
        if (selectedDate.Date == game.CurrentDate.Date) detail += "  / TODAY";
        if (selectedDate.Date < game.CurrentDate.Date) detail += "  / PAST";
        var selectedEntries = Entries(selectedDate);
        calendarDetail.text = "<b>" + detail + "</b>\n\n" + (selectedEntries.Count > 0 ? string.Join("\n\n", selectedEntries) : "No special appointment.") +
            "\n\nFood and essentials settle every evening. Tap another date for details.";
        if ((selectedDate - game.CurrentDate.AddDays(1-game.DayNumber)).Days >= game.TotalDays)
            calendarDetail.text += "\n\nBeyond this run: recurring bills still shown.";
    }
}
