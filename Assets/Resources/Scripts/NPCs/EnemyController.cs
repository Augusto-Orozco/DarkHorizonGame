/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| EnemyController.cs | Behaviour component that decides when to chase or  |
| attack a player.                                                        |
|                                                                         |
\*=======================================================================*/

using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private ProceduralKharvekMovement movement;

    private Transform player;

    private void Awake()
    {
        if (combat == null)
            combat = GetComponent<EnemyCombat>();

        if (movement == null)
            movement = GetComponent<ProceduralKharvekMovement>();

        PlayerMovement playerMovement =
            FindFirstObjectByType<PlayerMovement>();

        if (playerMovement != null)
            player = playerMovement.transform;

        if (movement != null)
            movement.enabled = true;
    }

    private void Update()
    {
        if (data == null || player == null || combat == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        bool inRange = distance <= data.DetectionRange;

        if (movement != null)
            movement.enabled = inRange;

        if (inRange)
            combat.TryAttack();
    }
}
