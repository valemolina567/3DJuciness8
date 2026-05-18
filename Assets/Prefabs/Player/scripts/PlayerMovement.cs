using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;

    [Header("Mouse")]
    public float mouseSensitivity = 100f;
    public Transform cameraPivot;

    [Header("Salto")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundLayer;

    [Header("Animación")]
    public Animator animator;

    private float xRotation = 0f;
    private Rigidbody rb;

    private float moveX;
    private float moveZ;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // INPUT
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        // ROTACIÓN CON MOUSE
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

       // DETECCIÓN DE SUELO
isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

// Detectar si se mueve
bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;

// SALTO (input)
if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
{
    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
}

// ANIMACIONES
if (!isGrounded)
{
    // En el aire
    animator.SetBool("Saltando", true);
    animator.SetBool("Corriendo", false); // evita conflicto visual
}
else
{
    // En el suelo
    animator.SetBool("Saltando", false);
    animator.SetBool("Corriendo", isMoving);
}
    }

    void FixedUpdate()
    {
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        Vector3 velocity = move.normalized * speed;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }
}