using UnityEngine;

public class ZombShark : MonoBehaviour
{
    public float speed = 3f;
    public float directionChangeInterval = 1.5f;
    public float repulsionRadius = 5f;
    public float sampleSpacing = 0.5f;
    public float repulsionStrength = 5f;

    public MapGenerator mapGenerator;

    private float timer;
    private Vector2 direction;

    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    void Start() {
        Debug.Log("Sharkie swimming :)");
        direction = Random.insideUnitCircle.normalized;
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= directionChangeInterval)
        {
            float angle = Random.Range(-45f, 45f);
            direction = RotateVector(direction, angle);
            timer = 0.0f;
        }

        Vector2 repulsion = RepelFromWalls();
        Vector2 finalDir = (direction + repulsion).normalized;

        Vector3 move = new Vector3(finalDir.x, 0.0f, finalDir.y);
        Vector3 nextPos = transform.position + move * speed * Time.deltaTime;

        // clamp within bounds
        nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
        nextPos.y = 0.0f;
        nextPos.z = Mathf.Clamp(nextPos.z, minY, maxY);

        transform.position = nextPos;
    }

    Vector2 RotateVector(Vector2 direction, float angle) {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    Vector2 RepelFromWalls() {
        Vector2 repel = Vector2.zero;
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.z); // FIXED

        for (float dx = -repulsionRadius; dx <= repulsionRadius; dx += sampleSpacing)
        {
            for (float dy = -repulsionRadius; dy <= repulsionRadius; dy += sampleSpacing)
            {
                Vector2 offset = new Vector2(dx, dy);
                Vector2 samplePoint = currentPos + offset;

                float distance = offset.magnitude;
                if (distance > 0.01f && distance < repulsionRadius)
                {
                    int tileType = mapGenerator.GetTileType(samplePoint);

                    // Repel from cave (0), wall (1), or border (3)
                    if (tileType == 0 || tileType == 1 || tileType == 3)
                    {
                        float strength = (repulsionRadius - distance) / repulsionRadius;

                        float typeMultiplier = tileType switch
                        {
                            3 => 2f,
                            1 => 2f,
                            0 => 1f,
                            -1 => 1f,
                            _ => 1f
                        };

                        repel += offset.normalized * -strength * typeMultiplier;
                    }
                }
            }
        }

        return repel.normalized * repulsionStrength;
    }

}
