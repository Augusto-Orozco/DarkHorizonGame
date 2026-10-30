using UnityEngine;
using Photon.Pun;


[RequireComponent(typeof(PhotonView))]
public class NetworkMovementAnimation : MonoBehaviourPun
{
    [SerializeField] private PlayerMovement movement;
    [Tooltip("Velocidad minima para considerear que camina")]
    [SerializeField] private float movingThreshold = 0.5f;

    private Vector3 lastPosition;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if(photonView.IsMine || movement == null)
        {
            enabled = false;
            return;
        }

        movement.readInput = false;
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;  

        float speed = delta.magnitude / Time.deltaTime;
        movement.SetMovingExternally(speed > movingThreshold);

        lastPosition = transform.position;
    }
}
