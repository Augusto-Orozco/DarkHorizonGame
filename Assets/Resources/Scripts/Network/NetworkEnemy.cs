using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkEnemy : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    public const byte RoleNormal = 0;
    public const byte RoleBoss = 1;
    public const byte RoleEscort = 2;

    [SerializeField] private Behaviour[] masterOnly;

    public static bool IsAuthority => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    public byte Role { get; private set; }
    private bool isSetup;
    private bool[] originalEnabled;

    private void Awake()
    {
        if (masterOnly != null)
        {
            originalEnabled = new bool[masterOnly.Length];
            for (int i = 0; i < masterOnly.Length; i++)
                originalEnabled[i] = masterOnly[i] != null && masterOnly[i].enabled;
        }

        if (!IsAuthority)
            SetMasterOnly(false);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        SetMasterOnly(true);
        Debug.Log($"[RED] {name}: este jugador es el nuevo host, la IA del enemigo ahora corre aqui.", this);
    }

    private void SetMasterOnly(bool value)
    {
        if (masterOnly == null)
            return;

        for (int i = 0; i < masterOnly.Length; i++)
        {
            if (masterOnly[i] != null)
                masterOnly[i].enabled = value && originalEnabled[i];
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