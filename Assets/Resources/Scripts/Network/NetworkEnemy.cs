using Photon.Pun;
using UnityEngine;

public class NetworkEnemy : MonoBehaviour, IPunInstantiateMagicCallback
{
    public const byte RoleNormal = 0;
    public const byte RoleBoss = 1;
    public const byte RoleEscort = 2;

    [Tooltip("Scripts que SOLO corren en el Master (IA, daño, drop). En los demás se apagan.")]
    [SerializeField] private Behaviour[] masterOnly;

    public static bool IsAuthority => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    public byte Role { get; private set; }
    private bool isSetup;

    private void Awake()
    {
        if (IsAuthority)
            return;

        foreach (Behaviour behaviour in masterOnly)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Setup(info.photonView.InstantiationData);
    }

    public static object[] BuildData(byte role, float scale, float healthMultiplier, Color color, string displayName)
    {
        return new object[] { role, scale, healthMultiplier, color.r, color.g, color.b, displayName ?? "" };
    }

    public void Setup(object[] data)
    {
        if (isSetup || data == null || data.Length < 7)
            return;

        isSetup = true;

        Role = (byte)data[0];
        float scale = (float)data[1];
        float healthMultiplier = (float)data[2];
        Color color = new Color((float)data[3], (float)data[4], (float)data[5]);
        string displayName = (string)data[6];

        if (!Mathf.Approximately(scale, 1f))
            transform.localScale *= scale;


        EnemyHealth health = GetComponentInChildren<EnemyHealth>();
        if (health == null)
            return;

        if (!Mathf.Approximately(healthMultiplier, 1f))
            health.ScaleMaxHealth(healthMultiplier);

        if (Role == RoleBoss)
        {
            EnemyBoss boss = health.gameObject.AddComponent<EnemyBoss>();
            boss.SetAppearance(color, displayName);
        }
    }
}