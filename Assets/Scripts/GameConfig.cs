using UnityEngine;

[CreateAssetMenu(menuName = "UnblockJamDemo/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Gameplay")]
    public float snapDistance = 1.25f;
    public float grindDuration = 0.9f;

    [Header("Audio Clips")]
    public AudioClip grinderClip;      // grinder.mp3
    public AudioClip stonePushClip;    // stone-push-37412.mp3
    public AudioClip dragLoopClip;     // object-slide...mp3
    public AudioClip[] moveClips;      // move-1/2/3.mp3

    [Header("Materials (optional)")]
    public Material whiteMat;
    public Material redMat;
    public Material blueMat;
    public Material greenMat;
    public Material yellowMat;
    public Material purpleMat;

    public Material GetMat(BlockColor c)
    {
        switch (c)
        {
            case BlockColor.Red: return redMat ? redMat : whiteMat;
            case BlockColor.Blue: return blueMat ? blueMat : whiteMat;
            case BlockColor.Green: return greenMat ? greenMat : whiteMat;
            case BlockColor.Yellow: return yellowMat ? yellowMat : whiteMat;
            case BlockColor.Purple: return purpleMat ? purpleMat : whiteMat;
            default: return whiteMat;
        }
    }
}
