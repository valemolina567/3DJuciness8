using UnityEngine;

public class Coin : MonoBehaviour
{
    public float floatAmplitude = 0.25f;
    public float floatSpeed = 2f;
    public float rotationSpeed = 90f;
    public float collectRiseDistance = 4f;
    public float collectDuration = 0.35f;

    private Vector3 startPosition;
    private Vector3 initialScale;
    private bool isCollected;
    private Collider coinCollider;
    private Renderer[] renderers;
    private AudioSource audioSource;

    void Start()
    {
        startPosition = transform.position;
        initialScale = transform.localScale;
        coinCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>(true);
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isCollected)
            return;

        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPosition + Vector3.up * offsetY;
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected || !IsPlayerCollider(other))
            return;

        isCollected = true;

        if (coinCollider != null)
        {
            coinCollider.enabled = false;
        }

        PlayCollectSound();

        StartCoroutine(CollectAnimation());
    }

    bool IsPlayerCollider(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<FPSController>() != null;
    }

    System.Collections.IEnumerator CollectAnimation()
    {
        Vector3 collectStartPosition = transform.position;
        float soundDuration = GetCollectSoundDuration();
        float elapsed = 0f;

        while (elapsed < collectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / collectDuration);

            transform.position = collectStartPosition + Vector3.up * (collectRiseDistance * progress);
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            SetAlpha(1f - progress);

            yield return null;
        }

        float remainingSoundTime = soundDuration - collectDuration;
        if (remainingSoundTime > 0f)
        {
            yield return new WaitForSeconds(remainingSoundTime);
        }

        Destroy(gameObject);
    }

    void PlayCollectSound()
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        audioSource.Play();
    }

    float GetCollectSoundDuration()
    {
        if (audioSource == null || audioSource.clip == null)
            return 0f;

        float pitch = Mathf.Abs(audioSource.pitch);
        if (pitch <= Mathf.Epsilon)
            return audioSource.clip.length;

        return audioSource.clip.length / pitch;
    }

    void SetAlpha(float alpha)
    {
        for (int index = 0; index < renderers.Length; index++)
        {
            Material[] materials = renderers[index].materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                if (material.HasProperty("_BaseColor"))
                {
                    Color color = material.GetColor("_BaseColor");
                    color.a = alpha;
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }
        }
    }
}
