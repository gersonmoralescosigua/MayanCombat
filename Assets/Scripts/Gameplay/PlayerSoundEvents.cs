using UnityEngine;
using Fusion;

public class PlayerSoundEvents : NetworkBehaviour
{
    public AudioSource audioSource;

    [Header("Movement Sounds")]
    public AudioClip walkClip;
    public AudioClip jumpClip;

    [Header("Combat Sounds")]
    public AudioClip attackClip;

    [Header("Death Sounds")]
    public AudioClip mayaDeathClip;
    public AudioClip spanishDeathClip;

    public bool isMaya;

    public override void Spawned()
    {
        // Garantiza que solo el jugador local reproduce sonidos
        if (!Object.HasInputAuthority)
        {
            audioSource.enabled = false;
        }
    }

    // --- Estos métodos se llaman desde los Animation Events ---

    public void PlayWalk()     { Play(walkClip); }
    public void PlayJump()     { Play(jumpClip); }
    public void PlayAttack()   { Play(attackClip); }

    public void PlayDeath()
    {
        if (isMaya) Play(mayaDeathClip);
        else Play(spanishDeathClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}
