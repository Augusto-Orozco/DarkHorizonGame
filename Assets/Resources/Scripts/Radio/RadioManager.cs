/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 21-10-2026 | 
|                                                                         |
| RadioManager.cs | Script that administrates the dialague system for the |
| commander NPC                                                           |
|                                                                         |
\*=======================================================================*/


using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadioManager : MonoBehaviour
{
    [Header("Interface")]
    [SerializeField] private GameObject radioPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image portrait;

    [Header("Configuration")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 35f; //Determines typing script

    [Header("Portrait Bright")]
    [SerializeField] private Color idleColor = Color.white;

    [SerializeField]
    private Color speakingColor = new Color(1f, 1f, 0.8f, 1f);

    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (radioPanel != null)
            radioPanel.SetActive(false);

        if (portrait != null)
            portrait.color = idleColor;
    }

    /*--<summary>------------------------------------------------------------------\
    | Allows any other object to request for the comunication.                     |
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \-----------------------------------------------------------------------------*/

    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (radioPanel == null || messageText == null)
        {
            Debug.LogWarning(
                "RadioManager: falta asignar el panel o el texto.",
                this
            );
            return;
        }

        radioPanel.SetActive(true);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeMessage(message));
    }

    /*--<summary>------------------------------------------------------------------\
    | Reveals the characters progresively and changes the portrait alpha value     |
    | while typing, giving a "talking" ilusion.                                    |
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \-----------------------------------------------------------------------------*/

    private IEnumerator TypeMessage(string message)
    {
        messageText.text = message;
        messageText.maxVisibleCharacters = 0;

        if (portrait != null)
            portrait.color = speakingColor;

        float elapsed = 0f;
        int visibleCharacters = 0;

        while (visibleCharacters < messageText.textInfo.characterCount)
        {
            elapsed += Time.deltaTime;

            visibleCharacters = Mathf.Min(
                Mathf.FloorToInt(elapsed * charactersPerSecond),
                messageText.textInfo.characterCount
            );

            messageText.maxVisibleCharacters = visibleCharacters;

            yield return null;
        }

        messageText.maxVisibleCharacters =
            messageText.textInfo.characterCount;

        if (portrait != null)
            portrait.color = idleColor;

        typingCoroutine = null;
    }
}
