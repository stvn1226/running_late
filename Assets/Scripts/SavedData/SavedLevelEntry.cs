using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavedLevelEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelNameDisplay;
    [SerializeField] private TextMeshProUGUI levelSeedDisplay;
    [SerializeField] public string levelName;
    [SerializeField] public int levelSeed;
    [SerializeField] public Button startButton;
    [SerializeField] public Button renameButton;
    [SerializeField] public Button deleteButton;

    private SavedLevelsPanel parent;

    //if startButton is pressed, start the level with the given seed

    private void Start()
    {
        //string pureLevelSeedDisplay = levelSeedDisplay.text.Replace("Seed: ", "");
        //levelNameDisplay.text = levelName;
        //levelSeed = int.Parse(pureLevelSeedDisplay);

        parent = GetComponentInParent<SavedLevelsPanel>();

        startButton.onClick.AddListener(StartLevel);
    }

    private void StartLevel()
    {
        parent.OnPlayClicked();
    }
}
