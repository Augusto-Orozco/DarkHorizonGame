using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class SpectatorCamera : MonoBehaviour
{
    [SerializeField] private GameObject labelRoot;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private string labelFormat = "Espectando a {0}\n[Clic] Siguiente";
    [SerializeField, Min(0f)] private float startDelay = 2f;
    [SerializeField, Min(1f)] private float followSpeed = 15f;

    private readonly List<PlayerHealth> candidates = new List<PlayerHealth>();
    private ThirdPersonCamera thirdPerson;
    private PlayerHealth ownerHealth;
    private Vector3 localOffset;
    private Quaternion localRotation;
    private PlayerHealth target;
    private bool spectating;
    private float deadTime = -1f;

    private void Awake()
    {
        thirdPerson = GetComponent<ThirdPersonCamera>();
        ownerHealth = GetComponentInParent<PlayerHealth>();
        localOffset = transform.localPosition;

        if (labelRoot != null)
            labelRoot.SetActive(false);
    }

    private void Start()
    {
        PhotonView view = GetComponentInParent<PhotonView>();
        if (ownerHealth == null || (PhotonNetwork.InRoom && view != null && !view.IsMine))
            enabled = false;
    }

    private void Update()
    {
        if (!spectating)
        {
            TryStartSpectating();
            return;
        }

        if (target == null || target.IsDead)
            NextTarget();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            NextTarget();
    }

    private void LateUpdate()
    {
        if (!spectating || target == null)
            return;

        Transform followed = target.transform;
        Vector3 position = followed.TransformPoint(localOffset);
        Quaternion rotation = followed.rotation * localRotation;

        float t = followSpeed * Time.deltaTime;
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, position, t),
            Quaternion.Slerp(transform.rotation, rotation, t));
    }

    private void TryStartSpectating()
    {
        if (!ownerHealth.IsDead || !ownerHealth.IsOut)
        {
            deadTime = -1f;
            return;
        }

        TraitorGameManager manager = TraitorGameManager.Instance;
        if (manager == null || manager.IsGameOver)
            return;

        if (deadTime < 0f)
            deadTime = Time.time;

        if (Time.time - deadTime < startDelay)
            return;

        StartSpectating();
    }

    private void StartSpectating()
    {
        spectating = true;
        localRotation = transform.localRotation;

        if (thirdPerson != null)
            thirdPerson.enabled = false;

        transform.SetParent(null, true);
        NextTarget();
    }

    private void NextTarget()
    {
        candidates.Clear();
        foreach (PlayerHealth player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (player != ownerHealth && !player.IsDead)
                candidates.Add(player);
        }

        if (candidates.Count == 0)
        {
            target = null;
            UpdateLabel();
            return;
        }

        candidates.Sort((a, b) => GetActor(a).CompareTo(GetActor(b)));

        int index = target != null ? candidates.IndexOf(target) : -1;
        target = candidates[(index + 1) % candidates.Count];
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (labelRoot != null)
            labelRoot.SetActive(spectating && target != null);

        if (labelText != null && target != null)
            labelText.text = string.Format(labelFormat, GetDisplayName(target));
    }

    private static int GetActor(PlayerHealth player)
    {
        PhotonView view = player.GetComponent<PhotonView>();
        return view != null ? view.OwnerActorNr : 0;
    }

    private static string GetDisplayName(PlayerHealth player)
    {
        PhotonView view = player.GetComponent<PhotonView>();
        if (view != null && view.Owner != null && !string.IsNullOrEmpty(view.Owner.NickName))
            return view.Owner.NickName;

        return $"Jugador {GetActor(player)}";
    }

    private void OnDestroy()
    {
        if (spectating && labelRoot != null)
            labelRoot.SetActive(false);
    }
}