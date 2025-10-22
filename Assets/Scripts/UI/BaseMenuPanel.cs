using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class BaseMenuPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] protected GameObject panelRoot;
    [SerializeField] protected Button firstSelectedButton;
    
    protected bool isActive = false;
    
    public virtual void Show()
    {
        panelRoot.SetActive(true);
        isActive = true;
        OnShow();
        
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }
    
    public virtual void Hide()
    {
        panelRoot.SetActive(false);
        isActive = false;
        OnHide();
        
        EventSystem.current.SetSelectedGameObject(null);
    }
    
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    
    protected virtual void Awake()
    {
        SetupButtons();
    }
    
    protected virtual void OnEnable()
    {
        RegisterButtonCallbacks();
    }
    
    protected virtual void OnDisable()
    {
        UnregisterButtonCallbacks();
    }
    
    protected abstract void SetupButtons();
    protected abstract void RegisterButtonCallbacks();
    protected abstract void UnregisterButtonCallbacks();
    
    public bool IsActive => isActive;
}