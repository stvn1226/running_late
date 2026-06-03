using UnityEngine;
using UnityEngine.UI;

public class ExpressionsController : MonoBehaviour
{
    private RawImage rawImage;
    public Texture[] expressions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    public void SwitchImage(Texture newTexture)
    {
        rawImage.texture = newTexture;
    }

    public void SwitchImageByNumber(int textureNumber)
    {
        Texture newTexture = expressions[textureNumber];
        rawImage.texture = newTexture;
    }
}
