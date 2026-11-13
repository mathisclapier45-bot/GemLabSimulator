using System.Collections.Generic;
using UnityEngine;

public class MuseumPedestal : MonoBehaviour
{
    [SerializeField] private int rank;
    [SerializeField] private SpriteRenderer iconRenderer;

    private bool playerInRange;
    private GemDefinition displayedGem;

    private void Start()
    {
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged -= Refresh;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        Sfx.Play(Sfx.Id.ToolOpen);
        if (displayedGem == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Piédestal vide — continuez à explorer pour l'ajouter au musée !");
            return;
        }

        ToolFeedbackUI.Instance?.ShowMessage($"🏛 {displayedGem.displayName} — {displayedGem.description}");
    }

    private void Refresh()
    {
        displayedGem = GetTopDiscoveredGem(rank);
        if (iconRenderer != null)
            iconRenderer.sprite = displayedGem != null ? displayedGem.GetIcon() : null;
    }

    private static GemDefinition GetTopDiscoveredGem(int rank)
    {
        var database = GemDatabase.Instance;
        var codex = GemCodex.Instance;
        if (database == null || codex == null) return null;

        var discovered = new List<GemDefinition>();
        foreach (var def in database.allGems)
        {
            if (def != null && codex.IsDiscovered(def.name))
                discovered.Add(def);
        }

        discovered.Sort((a, b) => b.baseValue.CompareTo(a.baseValue));
        return rank < discovered.Count ? discovered[rank] : null;
    }
}
