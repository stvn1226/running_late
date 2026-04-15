using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the OptionSelect sound when the attached Button is clicked.
/// Add this component to any menu Button alongside its Button component.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuSoundTrigger : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
    }

    /// <summary>
    /// Plays the OptionSelect sound via the AudioManager.
    /// </summary>
    private void OnButtonClicked()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.OptionSelect);
    }
}
