using UnityEngine;

public class TNTScript : MonoBehaviour
{
    public float speed = 7;
    private Rigidbody2D rb2D;
    public Transform target;

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        rb2D.MovePosition(Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime));
    }
}
