using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(Camera))]
public class SpectatorCamera : MonoBehaviour
{
    private const string TextTable = "Textos";

    [SerializeField] private GameObject labelRoot;
    [SerializeField] private TMP_Text labelText;

    // Textos de la tabla "Textos". Ya traen su clave por defecto; "Espectando a {0}" y "Jugador {0}"
    // llevan {0}.
    [SerializeField] private LocalizedString spectatingText = new LocalizedString(TextTable, "G100");
    [SerializeField] private LocalizedString nextHintText = new LocalizedString(TextTable, "G101");
    [SerializeField] private LocalizedString playerNameText = new LocalizedString(TextTable, "G071");

    [SerializeField, Min(0f)] private float startDelay = 2f;
    [SerializeField, Min(1f)] private float followSpeed = 15f;

    private readonly List<PlayerHealth> candidates = new List<PlayerHealth>();
    private PlayerInput playerInput;
    private InputAction nextAction;
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

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Si se cambia el idioma mientras se espectea, el texto se vuelve a escribir.
    private void OnLocaleChanged(Locale locale)
    {
        if (spectating)
            UpdateLabel();
    }

    private void Start()
    {
        PhotonView view = GetComponentInParent<PhotonView>();
        if (ownerHealth == null || (PhotonNetwork.InRoom && view != null && !view.IsMine))
            enabled = false;

        playerInput = ownerHealth.GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            InputActionMap playerMap =
                playerInput.actions.FindActionMap("Player", true);

            nextAction = playerMap.FindAction("Next", true);
        }
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

        bool nextPressed =
            (nextAction != null && nextAction.WasPressedThisFrame()) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (nextPressed)
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
            labelText.text = spectatingText.GetLocalizedString(GetDisplayName(target)) + "\n" + nextHintText.GetLocalizedString();
    }

    private static int GetActor(PlayerHealth player)
    {
        PhotonView view = player.GetComponent<PhotonView>();
        return view != null ? view.OwnerActorNr : 0;
    }

    private string GetDisplayName(PlayerHealth player)
    {
        PhotonView view = player.GetComponent<PhotonView>();
        if (view != null && view.Owner != null && !string.IsNullOrEmpty(view.Owner.NickName))
            return view.Owner.NickName;

        return playerNameText.GetLocalizedString(GetActor(player));
    }

    private void OnDestroy()
    {
        if (spectating && labelRoot != null)
            labelRoot.SetActive(false);
    }
}