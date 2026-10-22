/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 21-10-2026 | 
|                                                                         |
| RadioManager.cs | Script that administrates the dialague system for the |
| commander NPC, dissapears after a set time and only is triggered once.  |
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
    [SerializeField] private RawImage portrait;

    [Header("Configuration")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 15f; //Determines typing script

    [SerializeField, Min(0f)]
    private float displayDuration = 4f; //Duration of the display message

    [Header("Portrait Bright")]
    [SerializeField] private Color idleColor = Color.white;

    [SerializeField]
    private Color speakingColor = new Color(1f, 1f, 0.8f, 1f);

    private Coroutine messageCoroutine;

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
                "RadioManager: Unassigned Panel or text",
                this
            );
            return;
        }

        // Avoids simultaneus messages
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        radioPanel.SetActive(true);

        messageCoroutine = StartCoroutine(
            DisplayMessage(message)
        );
    }

    /*--<summary>------------------------------------------------------------------\
    | Reveals the characters progresively and changes the portrait alpha value     |
    | while typing, giving a "talking" ilusion.                                    |
    |--<returns>-------------------------------------------------------------------|
    |...                                                                           |
    \-----------------------------------------------------------------------------*/

    private IEnumerator DisplayMessage(string message)
    {
        messageText.text = message;
        messageText.maxVisibleCharacters = 0;
        messageText.ForceMeshUpdate();

        if (portrait != null)
            portrait.color = speakingColor;

        float elapsed = 0f;
        int totalCharacters = messageText.textInfo.characterCount;

        while (elapsed < totalCharacters / charactersPerSecond)
        {
            elapsed += Time.deltaTime;

            int visibleCharacters = Mathf.Min(
                Mathf.FloorToInt(elapsed * charactersPerSecond),
                messageText.textInfo.characterCount
            );

            messageText.maxVisibleCharacters = visibleCharacters;

            yield return null;
        }

        // Show the complete message
        messageText.maxVisibleCharacters = totalCharacters;

        if (portrait != null)
            portrait.color = idleColor;

        // Wait for the configurated display time.
        yield return new WaitForSeconds(displayDuration);

        // Hide the radio Window.
        radioPanel.SetActive(false);

        messageCoroutine = null;
    }
}
