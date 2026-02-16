using UnityEngine;

public class Sfx : MonoBehaviour
{
    public static Sfx I { get; private set; }

    [Header("Move Sound (Single Clip)")]
    [SerializeField] private AudioClip moveClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource src;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = false;
    }

    // Plays exactly one user-selected sound.
    public void PlayMove()
    {
        if (moveClip == null) return;
        src.PlayOneShot(moveClip, volume);
    }
}
