using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private Rigidbody rb;
    [SerializeField] Transform groundCheck;
    
    [Header("Props:")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private Vector3 _moveDir;
    private Vector3 _velocity;
    
    private Transform _transform;
    
    private bool _doJump = true;
    private bool _isGrounded = false;

    private void Start()
    {
        _transform = transform;
    }

    void Update()
    {
        _moveDir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (Input.GetButtonDown("Jump"))
        {
            _doJump = true;
        }
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        Vector3 move = _transform.right * _moveDir.x + _transform.forward * _moveDir.z;
        _velocity = move * speed * Time.fixedDeltaTime;

        _velocity.y = rb.velocity.y + Physics.gravity.y * Time.fixedDeltaTime;

        if (_doJump && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            _doJump = false;
        }

        rb.velocity = _velocity;
    }

    public bool IsMoving()
    {
        return Vector3.Magnitude(_moveDir) > 0.1f;
    }
}
