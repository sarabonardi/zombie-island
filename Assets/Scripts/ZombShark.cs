using UnityEngine;

public class ZombShark : MonoBehaviour
{
    public float speed = 3.0f;
    public float directionChangeInterval = 1.0f;
    public float repulsionRadius = 100f;
    public float density = 0.5f;
    public float repulsionStrength = 10f;

    public MapGenerator mapGenerator;
    private float timer;
    private Vector2 direction;

    void Start() {
        Debug.Log("Sharkie swimming :)");
        direction = Random.insideUnitCircle.normalized;
    }

    void Update() {
        timer += Time.deltaTime;

        // periodically rotate slightly to simulate swimming patterns
        if (timer >= directionChangeInterval) {
            float angle = Random.Range(-60f, 60f);
            direction = RotateVector(direction, angle);
            timer = 0.0f;
        }

        // calculate new direction from rotation and repelling from walls
        Vector2 repulsion = RepelFromWalls();
        Vector2 finalDir = (direction + repulsion).normalized;

        Vector3 move = new Vector3(finalDir.x, 0.0f, finalDir.y);
        Vector3 nextPos = transform.position + move * speed * Time.deltaTime;

        transform.position = nextPos;
    }

    Vector2 RotateVector(Vector2 direction, float angle) {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        // apply 2D rotation formula to x and y to get new direction vector
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    Vector2 RepelFromWalls() {
        Vector2 repel = Vector2.zero;
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.z); // FIXED

        // sample points from shark to radius length away
        for (float dx = -repulsionRadius; dx <= repulsionRadius; dx += density) {
            for (float dy = -repulsionRadius; dy <= repulsionRadius; dy += density) {
                Vector2 offset = new Vector2(dx, dy);
                Vector2 testPoint = currentPos + offset;

                float distance = offset.magnitude;
                if (distance > 0.01f && distance < repulsionRadius) {
                    int tileType = mapGenerator.GetTileType(testPoint);

                    // repel if tile = cave, wall, border, or oob
                    if (tileType == 0 || tileType == 1 || tileType == 3) {
                        float strength = (repulsionRadius - distance) / repulsionRadius * 10;

                        // repel harder from borders than land just to be safe
                        float multiplier = tileType switch {
                            3 => 5.0f,
                            1 => 1.5f,
                            0 => 1.2f,
                            -1 => 5.0f,
                            _ => 1.0f
                        };
                        repel += -offset.normalized * strength * multiplier;
                    }
                }
            }
        }
        return repel.normalized * repulsionStrength;
    }

}
