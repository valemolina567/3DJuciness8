using UnityEngine;

public class CubeParticles : MonoBehaviour
{
    [Header("Source")]
    public GameObject sourceObject;
    public bool spawnOnStart = true;

    [Header("Spawn")]
    public int instanceCount = 8;
    public float spawnRadius = 2f;
    public Vector2 spawnDelayRange = new Vector2(0f, 0.35f);
    public bool includeCenterInstance = true;

    [Header("Animation")]
    public Vector2 randomScaleMultiplierRange = new Vector2(0.8f, 1.3f);
    public float initialBurstSpeed = 4f;
    public float appearDuration = 0.2f;
    public float lifetime = 1.5f;
    public float destroyAfterSeconds = 2f;
    public float riseDistance = 1.5f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private bool hasSpawned;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnParticles();
        }
    }

    public void SpawnParticles()
    {
        if (hasSpawned || sourceObject == null)
            return;

        hasSpawned = true;

        if (destroyAfterSeconds > 0f)
        {
            Destroy(gameObject, destroyAfterSeconds);
        }

        Vector3 sourceScale = sourceObject.transform.localScale;
        Quaternion sourceRotation = sourceObject.transform.rotation;

        SpawnSourceObject(sourceScale);

        if (includeCenterInstance)
        {
            SpawnSingle(transform.position, sourceScale, sourceRotation);
        }

        for (int index = 0; index < instanceCount; index++)
        {
            SpawnSingle(GetRandomSpawnPosition(), sourceScale, sourceRotation);
        }
    }

    void SpawnSourceObject(Vector3 sourceScale)
    {
        if (sourceObject == gameObject)
        {
            SpawnSingle(GetRandomSpawnPosition(), sourceScale, sourceObject.transform.rotation);
            return;
        }

        Vector3 targetScale = Vector3.Scale(sourceScale, Vector3.one * Random.Range(randomScaleMultiplierRange.x, randomScaleMultiplierRange.y));
        Vector3 targetPosition = GetRandomSpawnPosition();

        sourceObject.transform.position = transform.position;
        sourceObject.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateInstance(sourceObject, transform.position, targetPosition, targetScale, Random.Range(spawnDelayRange.x, spawnDelayRange.y)));
    }

    void SpawnSingle(Vector3 targetPosition, Vector3 sourceScale, Quaternion sourceRotation)
    {
        GameObject instance = Instantiate(sourceObject, transform.position, sourceRotation, transform);
        Vector3 targetScale = Vector3.Scale(sourceScale, Vector3.one * Random.Range(randomScaleMultiplierRange.x, randomScaleMultiplierRange.y));

        instance.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateInstance(instance, transform.position, targetPosition, targetScale, Random.Range(spawnDelayRange.x, spawnDelayRange.y)));
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector2 circleOffset = Random.insideUnitCircle * spawnRadius;
        return transform.position + new Vector3(circleOffset.x, 0f, circleOffset.y);
    }

    System.Collections.IEnumerator AnimateInstance(GameObject instance, Vector3 originPosition, Vector3 targetPosition, Vector3 targetScale, float startDelay)
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        SetAlpha(renderers, 0f);

        Vector3 horizontalTarget = new Vector3(targetPosition.x, originPosition.y, targetPosition.z);
        float horizontalDistance = Vector3.Distance(originPosition, horizontalTarget);

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);
            float appearProgress = appearDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / appearDuration);

            float horizontalProgress = horizontalDistance <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01((initialBurstSpeed * elapsed) / horizontalDistance);

            Vector3 horizontalPosition = Vector3.Lerp(originPosition, horizontalTarget, horizontalProgress);

            instance.transform.position = horizontalPosition + Vector3.up * (riseDistance * normalizedTime);
            instance.transform.localScale = targetScale * scaleCurve.Evaluate(appearProgress);
            SetAlpha(renderers, fadeCurve.Evaluate(normalizedTime));

            yield return null;
        }

        Destroy(instance);
    }

    void SetAlpha(Renderer[] renderers, float alpha)
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
