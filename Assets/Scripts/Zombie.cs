using UnityEngine;

public class Zombie : MonoBehaviour
{
    public float patrolDistance = 3f;
    public float speed = 2f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool movingRight = true;

    void Start()
    {
        startPoint = transform.position - transform.right * patrolDistance / 2f;
        endPoint = transform.position + transform.right * patrolDistance / 2f;
    }

    void Update()
    {
        Vector3 target = movingRight ? endPoint : startPoint;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            movingRight = !movingRight;

            // Flip the sprite on Y axis (since X rotation is 90 degrees)
            Vector3 scale = transform.localScale;
            scale.x = movingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
