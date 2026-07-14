using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any UI Button to drive a player action on mobile.
/// Auto-connects to the spawned player via SpawnManager.OnPlayerSpawned.
/// No EventTrigger, no manual inspector wiring to the player needed.
///
/// Usage:
///   1. Add this component to your Button GameObject.
///   2. Pick the Action in the Inspector dropdown.
///   3. Done.
/// </summary>
public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonAction
    {
        Jump,
        Slide,
        Kick,
        Dive,
        DropDown
    }

    [Tooltip("Which action this button triggers.")]
    [SerializeField] private ButtonAction action = ButtonAction.Jump;

    public static int ActiveButtonTouches = 0;
    private bool isPressed = false;

    private PlayerControllerModular _controller;

    private void OnEnable()
    {
        SpawnManager.OnPlayerSpawned += OnPlayerSpawned;

        // In case the player was already spawned before this button enabled
        if (SpawnManager.Instance != null)
        {
            GameObject existing = SpawnManager.Instance.GetCurrentPlayer();
            if (existing != null)
                RegisterController(existing);
        }
    }

    private void OnDisable()
    {
        SpawnManager.OnPlayerSpawned -= OnPlayerSpawned;
        _controller = null;
        
        if (isPressed)
        {
            isPressed = false;
            ActiveButtonTouches--;
        }
    }

    private void OnPlayerSpawned(GameObject player) => RegisterController(player);

    private void RegisterController(GameObject player)
    {
        _controller = player.GetComponent<PlayerControllerModular>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isPressed)
        {
            isPressed = true;
            ActiveButtonTouches++;
        }

        if (_controller == null) return;

        switch (action)
        {
            case ButtonAction.Jump:     _controller.OnJumpButtonDown();     break;
            case ButtonAction.Slide:    _controller.OnSlideButtonDown();    break;
            case ButtonAction.Kick:     _controller.OnKickButton();         break;
            case ButtonAction.Dive:     _controller.OnDiveButton();         break;
            case ButtonAction.DropDown: _controller.OnDropDownButtonDown(); break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPressed)
        {
            isPressed = false;
            ActiveButtonTouches--;
        }

        if (_controller == null) return;

        switch (action)
        {
            case ButtonAction.Jump:     _controller.OnJumpButtonUp();     break;
            case ButtonAction.Slide:    _controller.OnSlideButtonUp();    break;
            case ButtonAction.DropDown: _controller.OnDropDownButtonUp(); break;
            // Kick and Dive are fire-and-forget, no release needed
        }
    }
}
