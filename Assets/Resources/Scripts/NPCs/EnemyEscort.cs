using UnityEngine;


public class EnemyEscort : MonoBehaviour
{
    private EnemyHealth boss;
    private EnemyTargeting targeting;
    private float alertDuration;
    private float alertRange;
    private float lastBossHealth;

    public EnemyHealth Boss => boss;

    public void Init(EnemyHealth bossHealth, Vector2 patrolHalfExtents, float alertDuration, float alertRange)
    {
        boss = bossHealth;
        this.alertDuration = alertDuration;
        this.alertRange = alertRange;

        targeting = GetComponent<EnemyTargeting>();
        if (targeting == null)
            targeting = gameObject.AddComponent<EnemyTargeting>();

        IPatrolArea patrol = GetComponent<IPatrolArea>();
        if (patrol != null)
            patrol.SetPatrolArea(boss.transform, patrolHalfExtents);

        lastBossHealth = boss.CurrentHealth;
        boss.OnHealthChanged += HandleBossHealthChanged;
    }

    private void HandleBossHealthChanged(float current, float max)
    {
        if (current < lastBossHealth)
            targeting.Alert(alertDuration, alertRange);

        lastBossHealth = current;
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged -= HandleBossHealthChanged;
    }
}