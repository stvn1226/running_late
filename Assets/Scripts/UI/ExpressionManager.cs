using UnityEngine;

public class ExpressionManager : MonoBehaviour
{
    public static ExpressionManager Instance { get; private set; }

    [SerializeField] private ExpressionsController expressionsController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SwitchExpression(int expressionNumber)
    {
        expressionsController.SwitchImageByNumber(expressionNumber - 1); // Subtract 1 to convert from 1-based to 0-based index
    }
}
