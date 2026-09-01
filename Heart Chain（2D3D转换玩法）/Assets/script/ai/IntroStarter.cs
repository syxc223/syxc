using UnityEngine;

public class IntroStarter : MonoBehaviour
{
    void Start()
    {
        var director = FindObjectOfType<ShapeCutsceneDirector>();
        if (director != null)
        {
            director.PlayIntroCutscene();
        }
    }
}