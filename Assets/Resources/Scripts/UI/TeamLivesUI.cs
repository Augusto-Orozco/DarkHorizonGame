using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamLivesUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image segmentTemplate;
    [SerializeField] private Image icon;
    [SerializeField] private float gapDegrees = 8f;
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color usedColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.25f;

    private readonly List<Image> segments = new List<Image>();
    private TraitorGameManager manager;
    private int maxLives;
    private int lastLives = -1;
    private Vector3 baseScale = Vector3.one;
    private Coroutine punch;

    private void Awake()
    {
        if (root == null || root == gameObject)
        {
            Debug.LogError("TeamLivesUI necesita un Root hijo (no el mismo objeto).", this);
            enabled = false;
            return;
        }

        if (segmentTemplate != null)
            segmentTemplate.gameObject.SetActive(false);

        baseScale = root.transform.localScale;
        SetVisible(false);
    }

    private void Update()
    {
        if (manager == null)
        {
            manager = TraitorGameManager.Instance;
            if (manager == null)
                return;

            manager.OnTeamLivesChanged += Show;
            Show(manager.TeamLives);
        }

        if (manager.ArtifactCollected && root.activeSelf)
            SetVisible(false);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnTeamLivesChanged -= Show;
    }

    private void Show(int lives)
    {
        bool lost = lastLives >= 0 && lives < lastLives;
        lastLives = lives;

        bool visible = lives >= 0 && !manager.ArtifactCollected;
        SetVisible(visible);
        if (!visible)
            return;

        if (lives > maxLives)
        {
            maxLives = lives;
            BuildSegments();
        }

        for (int i = 0; i < segments.Count; i++)
            segments[i].color = i < lives ? fullColor : usedColor;

        if (icon != null)
            icon.color = lives > 0 ? iconColor : emptyColor;

        if (lost)
        {
            if (punch != null)
                StopCoroutine(punch);
            punch = StartCoroutine(Punch());
        }
    }

    private void BuildSegments()
    {
        foreach (Image segment in segments)
        {
            if (segment != null)
                Destroy(segment.gameObject);
        }
        segments.Clear();

        if (segmentTemplate == null || maxLives <= 0)
            return;

        float slice = 360f / maxLives;
        float gap = maxLives > 1 ? Mathf.Clamp(gapDegrees, 0f, slice * 0.5f) : 0f;
        float direction = segmentTemplate.fillClockwise ? -1f : 1f;
        Transform parent = segmentTemplate.transform.parent;
        int siblingIndex = segmentTemplate.transform.GetSiblingIndex();

        for (int i = 0; i < maxLives; i++)
        {
            Image segment = Instantiate(segmentTemplate, parent);
            segment.name = $"Segmento{i + 1}";
            segment.gameObject.SetActive(true);
            segment.type = Image.Type.Filled;
            segment.fillMethod = Image.FillMethod.Radial360;
            segment.fillAmount = (slice - gap) / 360f;
            segment.transform.SetSiblingIndex(siblingIndex + 1 + i);
            segment.transform.localRotation = segmentTemplate.transform.localRotation *
                Quaternion.Euler(0f, 0f, direction * (slice * i + gap * 0.5f));
            segments.Add(segment);
        }
    }

    private IEnumerator Punch()
    {
        Transform target = root.transform;
        float t = 0f;

        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / punchDuration) * Mathf.PI);
            target.localScale = baseScale * Mathf.Lerp(1f, punchScale, k);
            yield return null;
        }

        target.localScale = baseScale;
        punch = null;
    }

    private void SetVisible(bool value)
    {
        if (root != null && root.activeSelf != value)
            root.SetActive(value);
    }
}