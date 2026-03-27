using UnityEngine;

public class PawnScript : MonoBehaviour
{
    public float speed = 5;
    private Animator animator;
    Rigidbody2D rb2D;
    Vector2 movementInput;

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;

        if (Mathf.Abs(movementInput.x) > 0.1 || Mathf.Abs(movementInput.y) > 0.1 )
        {            
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        CheckFlip();
    }

    private void FixedUpdate()
    {
        rb2D.linearVelocity = movementInput * speed;
    }

    private void CheckFlip()
    {
        if (movementInput.x > 0 && transform.localScale.x < 0  || movementInput.x < 0 && transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }
    }

}
