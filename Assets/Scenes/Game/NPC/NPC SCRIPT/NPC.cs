using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour, IInteractable
{
    public static event Action DialogueStateChanged;

    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    [SerializeField] private bool resizeDialoguePanelToFitText;

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
    private string[] activeDialogueLines;
    private string[] activeChoices;
    private Action<int> choiceCallback;
    private Action dialogueClosedCallback;
    private bool waitingForChoice;
    private bool isHelperDialogue;
    private bool restoreDialogueTextSizing;
    private bool dialogueTextAutoSizing;
    private float dialogueTextFontSize;
    private float reprimandDialogueDuration;
    private RectTransform dialoguePanelRect;
    private RectTransform dialogueTextRect;
    private RectTransform dialogueNameRect;
    private RectTransform dialoguePortraitRect;
    private Vector2 originalDialoguePanelSize;
    private Vector2 originalDialoguePanelPosition;
    private Vector2 originalDialogueTextSize;
    private Vector2 originalDialogueNamePosition;
    private Vector2 originalDialoguePortraitPosition;
    private bool dialogueLayoutCached;

    public bool IsDialogueActive => isDialogueActive;

    public static bool IsAnyDialogueActive
    {
        get
        {
            NPC[] npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
            foreach (NPC npc in npcs)
            {
                if (npc != null && npc.IsDialogueActive)
                    return true;
            }

            return false;
        }
    }

    public bool CanInteract()
    {
        return (dialogueData != null || GetComponent<HouseholdHelperAI>() != null) &&
            !MiniGameInteractionBlocker.IsAnyMiniGameActive &&
            (!PauseController.IsGamePaused || isDialogueActive);
    }

    public bool CanStartAutomaticDialogue =>
        !isDialogueActive &&
        !MiniGameInteractionBlocker.IsAnyMiniGameActive &&
        dialoguePanel != null &&
        dialogueText != null &&
        nameText != null;

    public static NPC FindDialogueHost(NPC preferred)
    {
        if (preferred != null && preferred.CanStartAutomaticDialogue)
            return preferred;

        NPC[] npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (NPC npc in npcs)
        {
            if (npc != null && npc.CanStartAutomaticDialogue)
                return npc;
        }

        return null;
    }

    public void Interact()
    {
        if (isDialogueActive)
        {
            NextLine();
            return;
        }

        if (!CanInteract())
            return;

        HouseholdHelperAI helper = GetComponent<HouseholdHelperAI>();
        if (helper != null)
            helper.Interact();
        else
            StartDialogue();
    }

    private void Update()
    {
        if (!waitingForChoice || Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard.digit0Key.wasPressedThisFrame ||
            keyboard.numpad0Key.wasPressedThisFrame ||
            keyboard.escapeKey.wasPressedThisFrame)
        {
            FinishChoice(-1);
            return;
        }

        for (int index = 0; index < activeChoices.Length && index < 9; index++)
        {
            bool pressed = index switch
            {
                0 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
                1 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
                2 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
                3 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
                4 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
                5 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
                6 => keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame,
                7 => keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame,
                _ => keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame
            };

            if (pressed)
            {
                FinishChoice(index);
                return;
            }
        }
    }

    public bool StartHelperDialogue(
        string greeting,
        string prompt,
        IReadOnlyList<string> choices,
        Action<int> onChoice)
    {
        if (isDialogueActive || MiniGameInteractionBlocker.IsAnyMiniGameActive)
            return false;

        if (!HasDialogueReferences())
            return false;

        activeDialogueLines = new[] { greeting, prompt };
        activeChoices = choices != null ? new List<string>(choices).ToArray() : Array.Empty<string>();
        choiceCallback = onChoice;
        dialogueClosedCallback = null;
        isHelperDialogue = true;
        dialogueIndex = 0;
        isDialogueActive = true;
        waitingForChoice = false;
        nameText.SetText(dialogueData != null && !string.IsNullOrWhiteSpace(dialogueData.npcName)
            ? dialogueData.npcName
            : name);
        if (portraitImage != null && dialogueData != null)
            portraitImage.sprite = dialogueData.npcPortrait;

        PrepareDialoguePanelLayout();
        dialoguePanel.SetActive(true);
        DialogueStateChanged?.Invoke();
        PauseController.SetPause(this, true);
        StartCoroutine(TypeLine());
        return true;
    }

    public bool StartAutomaticDialogue(
        string speakerName,
        string line,
        Sprite speakerPortrait,
        float displayDuration,
        Action onClosed)
    {
        if (isDialogueActive || MiniGameInteractionBlocker.IsAnyMiniGameActive)
            return false;

        if (!HasDialogueReferences())
            return false;

        activeDialogueLines = new[] { line };
        activeChoices = Array.Empty<string>();
        choiceCallback = null;
        dialogueClosedCallback = onClosed;
        reprimandDialogueDuration = displayDuration;
        isHelperDialogue = false;
        dialogueIndex = 0;
        isDialogueActive = true;
        waitingForChoice = false;
        nameText.SetText(speakerName);
        if (portraitImage != null && speakerPortrait != null)
            portraitImage.sprite = speakerPortrait;

        PrepareDialoguePanelLayout();
        dialoguePanel.SetActive(true);
        DialogueStateChanged?.Invoke();
        PauseController.SetPause(this, true);
        StartCoroutine(TypeLine());
        StartCoroutine(AutoCloseReprimandDialogue(displayDuration));
        return true;
    }

    private IEnumerator AutoCloseReprimandDialogue(float displayDuration)
    {
        while (isTyping)
            yield return null;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, displayDuration));
        if (isDialogueActive && dialogueClosedCallback != null)
            EndDialogue();
    }

    void StartDialogue()
    {
        if (dialogueData.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
        {
            Debug.LogWarning($"NPC '{name}' has no dialogue lines.", this);
            return;
        }

        if (!HasDialogueReferences())
            return;

        activeDialogueLines = dialogueData.dialogueLines;
        activeChoices = Array.Empty<string>();
        choiceCallback = null;
        dialogueClosedCallback = null;
        isHelperDialogue = false;
        waitingForChoice = false;
        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(dialogueData.npcName);
        if (portraitImage != null)
            portraitImage.sprite = dialogueData.npcPortrait;
        PrepareDialoguePanelLayout();
        dialoguePanel.SetActive(true);
        DialogueStateChanged?.Invoke();
        PauseController.SetPause(this, true);

        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (waitingForChoice)
            return;

        if (isTyping)
        {
            StopAllCoroutines();
            SoundEffectManager.StopVoice(); // Stop audio if player skips typing
            dialogueText.SetText(activeDialogueLines[dialogueIndex]);
            ResizeDialoguePanelToText();
            isTyping = false;
            if (dialogueClosedCallback != null)
                StartCoroutine(AutoCloseReprimandDialogue(reprimandDialogueDuration));
        }
        else if (++dialogueIndex < activeDialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else if (choiceCallback != null && activeChoices.Length > 0)
        {
            ShowChoices();
        }
        else
        {
            EndDialogue();
        }
    }

   IEnumerator TypeLine()
{
    isTyping = true;
    dialogueText.SetText("");
       ResizeDialoguePanelToText();

          string line = activeDialogueLines[dialogueIndex];
          foreach (char letter in line)
          {
              dialogueText.text += letter;
              ResizeDialoguePanelToText();

           if (letter != ' ' && dialogueData != null && dialogueData.voiceSound != null)
           {
               SoundEffectManager.PlayVoice(dialogueData.voiceSound, dialogueData.voicePitch);
           }

        float typingSpeed = dialogueData != null
            ? Mathf.Max(0f, dialogueData.typingSpeed)
            : 0.03f;
        yield return new WaitForSecondsRealtime(typingSpeed);
    }

    isTyping = false;

    if (!isHelperDialogue &&
        choiceCallback == null &&
        dialogueData != null &&
        dialogueData.autoProgressLines != null &&
        dialogueData.autoProgressLines.Length > dialogueIndex &&
        dialogueData.autoProgressLines[dialogueIndex])
    {
        yield return new WaitForSecondsRealtime(dialogueData.autoProgressDelay);
        NextLine();
    }
}

    private void ShowChoices()
    {
        StringBuilder choicesText = new StringBuilder("Choose a task (press its number):\n");
        for (int index = 0; index < activeChoices.Length; index++)
        {
            choicesText.Append(index + 1)
                .Append(". ")
                .AppendLine(activeChoices[index]);
        }
        choicesText.Append("0. Nothing for now / Esc to cancel");
        dialogueTextAutoSizing = dialogueText.enableAutoSizing;
        dialogueTextFontSize = dialogueText.fontSize;
        restoreDialogueTextSizing = true;
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 12f;
        dialogueText.fontSizeMax = Mathf.Min(dialogueTextFontSize, 22f);
        dialogueText.SetText(choicesText.ToString());
        float choiceFontSize = dialogueText.enableAutoSizing
            ? dialogueText.fontSizeMax
            : dialogueText.fontSize;
        float minimumChoiceTextHeight =
            (activeChoices.Length + 2) * choiceFontSize * 1.15f;
        ResizeDialoguePanelToText(minimumChoiceTextHeight);
        waitingForChoice = true;
    }

    private void PrepareDialoguePanelLayout()
    {
        if (!resizeDialoguePanelToFitText)
            return;

        if (!dialogueLayoutCached)
        {
            dialoguePanelRect = dialoguePanel.GetComponent<RectTransform>();
            dialogueTextRect = dialogueText.rectTransform;
            if (dialoguePanelRect == null || dialogueTextRect == null)
                return;

            dialogueNameRect = nameText != null &&
                nameText.rectTransform.IsChildOf(dialoguePanelRect)
                    ? nameText.rectTransform
                    : null;
            dialoguePortraitRect = portraitImage != null &&
                portraitImage.rectTransform.IsChildOf(dialoguePanelRect)
                    ? portraitImage.rectTransform
                    : null;
            originalDialoguePanelSize = dialoguePanelRect.sizeDelta;
            originalDialoguePanelPosition = dialoguePanelRect.anchoredPosition;
            originalDialogueTextSize = dialogueTextRect.sizeDelta;
            if (dialogueNameRect != null)
                originalDialogueNamePosition = dialogueNameRect.anchoredPosition;
            if (dialoguePortraitRect != null)
                originalDialoguePortraitPosition = dialoguePortraitRect.anchoredPosition;
            dialogueLayoutCached = true;
        }

        ResetDialoguePanelLayout();
    }

    private void ResizeDialoguePanelToText(float minimumTextHeight = 0f)
    {
        if (!resizeDialoguePanelToFitText || !dialogueLayoutCached ||
            dialogueText == null || dialoguePanelRect == null || dialogueTextRect == null)
            return;

        dialogueText.ForceMeshUpdate();
        float preferredTextHeight = dialogueText.GetPreferredValues(
            dialogueText.text,
            originalDialogueTextSize.x,
            0f).y;
        float textHeight = Mathf.Max(
            originalDialogueTextSize.y,
            preferredTextHeight + 24f,
            minimumTextHeight);
        float extraHeight = textHeight - originalDialogueTextSize.y;

        RectTransform canvasRect = dialoguePanelRect.parent as RectTransform;
        if (canvasRect != null)
        {
            float maximumPanelHeight = Mathf.Max(
                originalDialoguePanelSize.y,
                canvasRect.rect.height - 32f);
            extraHeight = Mathf.Min(
                extraHeight,
                maximumPanelHeight - originalDialoguePanelSize.y);
        }

        dialogueTextRect.sizeDelta = new Vector2(
            originalDialogueTextSize.x,
            originalDialogueTextSize.y + extraHeight);
        dialoguePanelRect.sizeDelta = new Vector2(
            originalDialoguePanelSize.x,
            originalDialoguePanelSize.y + extraHeight);
        dialoguePanelRect.anchoredPosition = new Vector2(
            originalDialoguePanelPosition.x,
            originalDialoguePanelPosition.y + extraHeight * 0.5f);
        if (dialogueNameRect != null)
        {
            dialogueNameRect.anchoredPosition = new Vector2(
                originalDialogueNamePosition.x,
                originalDialogueNamePosition.y + extraHeight * 0.5f);
        }
        if (dialoguePortraitRect != null)
        {
            dialoguePortraitRect.anchoredPosition = new Vector2(
                originalDialoguePortraitPosition.x,
                originalDialoguePortraitPosition.y + extraHeight * 0.5f);
        }
    }

    private void ResetDialoguePanelLayout()
    {
        if (!dialogueLayoutCached || dialoguePanelRect == null || dialogueTextRect == null)
            return;

        dialoguePanelRect.sizeDelta = originalDialoguePanelSize;
        dialoguePanelRect.anchoredPosition = originalDialoguePanelPosition;
        dialogueTextRect.sizeDelta = originalDialogueTextSize;
        if (dialogueNameRect != null)
            dialogueNameRect.anchoredPosition = originalDialogueNamePosition;
        if (dialoguePortraitRect != null)
            dialoguePortraitRect.anchoredPosition = originalDialoguePortraitPosition;
    }

    private void FinishChoice(int choiceIndex)
    {
        Action<int> callback = choiceCallback;
        dialogueClosedCallback = null;
        choiceCallback = null;
        waitingForChoice = false;
        isHelperDialogue = false;
        EndDialogue();
        callback?.Invoke(choiceIndex);
    }

    private bool HasDialogueReferences()
    {
        if (dialoguePanel == null || dialogueText == null || nameText == null)
        {
            Debug.LogWarning(
                "NPC dialogue cannot start because its panel, dialogue text, or name text is not assigned.",
                this);
            return false;
        }

        return true;
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        SoundEffectManager.StopVoice();
        isDialogueActive = false;
        isTyping = false;
        waitingForChoice = false;
        if (restoreDialogueTextSizing && dialogueText != null)
        {
            dialogueText.enableAutoSizing = dialogueTextAutoSizing;
            dialogueText.fontSize = dialogueTextFontSize;
        }
        ResetDialoguePanelLayout();
        restoreDialogueTextSizing = false;
        activeDialogueLines = null;
        activeChoices = Array.Empty<string>();
        if (dialogueText != null)
            dialogueText.SetText("");
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        DialogueStateChanged?.Invoke();
        PauseController.SetPause(this, false);
        Action<int> callback = choiceCallback;
        Action closedCallback = dialogueClosedCallback;
        choiceCallback = null;
        dialogueClosedCallback = null;
        callback?.Invoke(-1);
        closedCallback?.Invoke();
    }

    private void OnDisable()
    {
        ResetDialoguePanelLayout();
        PauseController.SetPause(this, false);
    }
}