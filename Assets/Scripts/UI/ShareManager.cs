using UnityEngine;

public class ShareManager : MonoBehaviour
{
    private const string SHARE_SUBJECT = "Running Late!";
    private const string SHARE_TEXT = "I'm playing Running Late! Can you beat my high score? Download it now!";
    private const string SHARE_URL = "https://play.google.com/store/apps/details?id=com.unbreakable.runninglate"; // TODO: Update with real link

    public void ShareGame()
    {
        GameLogger.Log("SHARE", "Initiating Native Share...");

#if UNITY_ANDROID || UNITY_IOS
        new NativeShare()
            .SetSubject(SHARE_SUBJECT)
            .SetText(SHARE_TEXT)
            .SetUrl(SHARE_URL)
            .SetCallback((result, shareTarget) => GameLogger.Log("SHARE", "Share result: " + result + ", selected app: " + shareTarget))
            .Share();
#else
        GameLogger.LogWarning("SHARE", "Native Share is only supported on Android and iOS.");
        Application.OpenURL(SHARE_URL);
#endif
    }
}
