using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerController player;

    public void Bind(PlayerController target)
    {
        player = target;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        player?.SetHackInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        player?.SetHackInput(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        player?.SetHackInput(false);
    }
}
