using UnityEngine;

public sealed class PlayerSkinApplier : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void Start()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        ApplySkin(ProgressionManager.Instance.GetSelectedSkin());
    }

    public void ApplySkin(SkinDefinition skin)
    {
        if (skin == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Material material = targetRenderers[i].material;
            material.color = skin.BaseColor;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", skin.EmissionColor * 1.6f);
            }
        }
    }
}
