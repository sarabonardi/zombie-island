using UnityEngine;

public class Zombie : MonoBehaviour
{
    public float patrolDistance = 7.0f;
    public float speed = 1.0f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool movingRight = true;

    void Start() {
        // define patrol limits given the initialized position
        startPoint = transform.position - transform.right * patrolDistance / 2f;
        endPoint = transform.position + transform.right * patrolDistance / 2f;
    }

    void Update() {
        // start is target when moving left, end is target when moving right
        Vector3 target = movingRight ? endPoint : startPoint;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // when close to target, change direction
        if (Vector3.Distance(transform.position, target) < 0.05f) {
            movingRight = !movingRight;

            // flip sprite along X axis to turn left or right
            Vector3 scale = transform.localScale;
            scale.x = movingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
