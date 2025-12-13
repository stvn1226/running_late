using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class BaseMainMenuPanel : MonoBehaviour
{
    [Header("Base Panel Settings")]
    [SerializeField] protected GameObject panelRoot;
    [SerializeField] protected Button defaultSelectedButton;
    [SerializeField] protected Selectable defaultSelected;
    
    protected MainMenuController menuController;
    protected bool isVisible = false;
    
    public virtual void Initialize(MainMenuController controller)
    {
        menuController = controller;
        SetupPanel();
        Hide();
    }
    
    public virtual void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            isVisible = true;
        }
        
        OnShow();
        SetDefaultSelection();
    }
    
    public virtual void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            isVisible = false;
        }
        
        OnHide();
    }
    
    protected virtual void SetDefaultSelection()
    {
        if (defaultSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
        }
        else if (defaultSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelected.gameObject);
        }
    }
    
    protected abstract void SetupPanel();
    
    protected virtual void OnShow() { }
    
    protected virtual void OnHide() { }
    
    public bool IsVisible => isVisible;
}