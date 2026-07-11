using UnityEngine;
using UnityEngine.EventSystems;

public class GemDropZone : MonoBehaviour, IDropHandler
{
    public IGemContainer TargetContainer;

    public void OnDrop(PointerEventData eventData)
    {
        var source = GemDragSource.CurrentDrag;
        if (source == null || source.Gem == null || TargetContainer == null) return;

        if (!ReferenceEquals(source.SourceContainer, TargetContainer))
        {
            var gem = source.Gem;

            if (!gem.IsIdentified)
            {
                ToolFeedbackUI.Instance?.ShowMessage("Seules les gemmes identifiées peuvent être rangées dans le coffre.");
            }
            else if (!TargetContainer.TryAddGem(gem))
            {
                ToolFeedbackUI.Instance?.ShowMessage("Espace insuffisant.");
            }
            else
            {
                source.SourceContainer.RemoveGem(gem);
            }
        }

        // The move above can trigger a Refresh() that destroys the dragged slot's
        // GameObject before Unity gets a chance to call OnEndDrag on it, which would
        // otherwise leave the floating drag icon stuck on screen. Clean up explicitly here.
        GemDragSource.CleanupDrag();
    }
}
