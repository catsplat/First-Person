using UnityEngine;

public class MovementVFXController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem sprintParticles;
    public ParticleSystem dashParticles;
    public ParticleSystem slideParticles;
    public ParticleSystem wallRunParticles;
    public ParticleSystem landParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dashClip;
    public AudioClip slideClip;
    public AudioClip landClip;
    public AudioClip wallRunLoop; // loopable

    [Header("Camera")]
    public Transform camHolder;
    public float sprintTilt = 3f;
    public float slideTilt = -8f;

    AudioSource wallRunAudioSource;

    void OnEnable()
    {
        MovementEvents.OnDashStart += HandleDashStart;
        MovementEvents.OnDashEnd += HandleDashEnd;
        MovementEvents.OnStartSprint += HandleStartSprint;
        MovementEvents.OnStopSprint += HandleStopSprint;
        MovementEvents.OnSlideStart += HandleSlideStart;
        MovementEvents.OnSlideEnd += HandleSlideEnd;
        MovementEvents.OnWallRunStart += HandleWallRunStart;
        MovementEvents.OnWallRunEnd += HandleWallRunEnd;
        MovementEvents.OnLandHard += HandleLand;
    }

    void OnDisable()
    {
        MovementEvents.OnDashStart -= HandleDashStart;
        MovementEvents.OnDashEnd -= HandleDashEnd;
        MovementEvents.OnStartSprint -= HandleStartSprint;
        MovementEvents.OnStopSprint -= HandleStopSprint;
        MovementEvents.OnSlideStart -= HandleSlideStart;
        MovementEvents.OnSlideEnd -= HandleSlideEnd;
        MovementEvents.OnWallRunStart -= HandleWallRunStart;
        MovementEvents.OnWallRunEnd -= HandleWallRunEnd;
        MovementEvents.OnLandHard -= HandleLand;
    }

    void HandleDashStart()
    {
        if (dashParticles) dashParticles.Play();
        if (audioSource && dashClip) audioSource.PlayOneShot(dashClip);
        if (camHolder) StartCoroutine(DoCameraKick(0.12f, 6f));
    }

    void HandleDashEnd() { /* optional end effects */ }

    void HandleStartSprint()
    {
        if (sprintParticles) sprintParticles.Play();
        if (camHolder) StartCoroutine(DoTilt(sprintTilt, 0.12f));
    }

    void HandleStopSprint()
    {
        if (sprintParticles) sprintParticles.Stop();
        if (camHolder) StartCoroutine(DoTilt(0f, 0.12f));
    }

    void HandleSlideStart()
    {
        if (slideParticles) slideParticles.Play();
        if (audioSource && slideClip) audioSource.PlayOneShot(slideClip);
        if (camHolder) StartCoroutine(DoTilt(slideTilt, 0.12f));
    }

    void HandleSlideEnd()
    {
        if (slideParticles) slideParticles.Stop();
        if (camHolder) StartCoroutine(DoTilt(0f, 0.12f));
    }

    void HandleWallRunStart()
    {
        if (wallRunParticles) wallRunParticles.Play();
        if (wallRunLoop && audioSource)
        {
            wallRunAudioSource = gameObject.AddComponent<AudioSource>();
            wallRunAudioSource.clip = wallRunLoop;
            wallRunAudioSource.loop = true;
            wallRunAudioSource.Play();
        }
    }

    void HandleWallRunEnd()
    {
        if (wallRunParticles) wallRunParticles.Stop();
        if (wallRunAudioSource) { wallRunAudioSource.Stop(); Destroy(wallRunAudioSource); }
    }

    void HandleLand()
    {
        if (landParticles) landParticles.Play();
        if (audioSource && landClip) audioSource.PlayOneShot(landClip);
        if (camHolder) StartCoroutine(DoCameraKick(0.18f, 8f));
    }

    System.Collections.IEnumerator DoTilt(float target, float time)
    {
        if (camHolder == null) yield break;
        Quaternion start = camHolder.localRotation;
        Quaternion end = Quaternion.Euler(target, 0f, 0f);
        float t = 0f;
        while (t < time)
        {
            camHolder.localRotation = Quaternion.Slerp(start, end, t / time);
            t += Time.deltaTime;
            yield return null;
        }
        camHolder.localRotation = end;
    }

    System.Collections.IEnumerator DoCameraKick(float duration, float magnitude)
    {
        if (camHolder == null) yield break;
        Vector3 orig = camHolder.localPosition;
        float t = 0f;
        while (t < duration)
        {
            Vector3 rand = (Random.insideUnitSphere * 0.02f) * magnitude;
            camHolder.localPosition = orig + rand;
            t += Time.deltaTime;
            yield return null;
        }
        camHolder.localPosition = orig;
    }
}
