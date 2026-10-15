using UnityEngine;

// Lives on the Canvas, so closing the overlay does not unsubscribe from new runs.
public sealed class OpeningTutorial : MonoBehaviour
{
    public GameManager game;
    public GameObject panel;
    private bool showOnNextRun = true;
    private void OnEnable() { if (game != null) game.StateChanged += CheckRun; }
    private void OnDisable() { if (game != null) game.StateChanged -= CheckRun; }
    private void Start() => CheckRun();
    private void CheckRun()
    {
        if (game == null || panel == null) return;
        if (game.HasEnded) { showOnNextRun = true; panel.SetActive(false); return; }
        if (!game.HasStarted || !showOnNextRun) return;
        showOnNextRun = false;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }
    public void Continue() => panel.SetActive(false);
}
