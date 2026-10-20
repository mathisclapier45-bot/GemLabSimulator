using UnityEngine;

// Shows its targets (a lab desk and the instrument on it) only once the player owns the tool.
public class OwnedToolGate : MonoBehaviour
{
    [SerializeField] private string toolId;
    [SerializeField] private GameObject[] targets;

    private PlayerOwnedTools tools;

    private void Start()
    {
        tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += Apply;
        Apply();
    }

    private void OnDestroy()
    {
        if (tools != null) tools.OnToolsChanged -= Apply;
    }

    private void Apply()
    {
        var visible = tools != null && tools.Has(toolId);
        foreach (var target in targets)
            if (target != null) target.SetActive(visible);
    }
}
