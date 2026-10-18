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
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

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
        if (!CanInteract())
            return;

        if (!isDialogueActive)
        {
            HouseholdHelperAI helper = GetComponent<HouseholdHelperAI>();
            if (helper != null)
                helper.Interact();
            else
                StartDialogue();
        }
        else
        {
            NextLine();
        }
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

        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);
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

        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);
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
        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);

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

       string line = activeDialogueLines[dialogueIndex];
       foreach (char letter in line)
       {
           dialogueText.text += letter;

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
        StringBuilder choicesText = new StringBuilder("Pumili ng gawain (pindutin ang numero):\n");
        for (int index = 0; index < activeChoices.Length; index++)
        {
            choicesText.Append(index + 1)
                .Append(". ")
                .AppendLine(activeChoices[index]);
        }
        choicesText.Append("0. Wala muna / Esc para batalin");
        dialogueTextAutoSizing = dialogueText.enableAutoSizing;
        dialogueTextFontSize = dialogueText.fontSize;
        restoreDialogueTextSizing = true;
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 12f;
        dialogueText.fontSizeMax = Mathf.Min(dialogueTextFontSize, 22f);
        dialogueText.SetText(choicesText.ToString());
        waitingForChoice = true;
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
        restoreDialogueTextSizing = false;
        activeDialogueLines = null;
        activeChoices = Array.Empty<string>();
        if (dialogueText != null)
            dialogueText.SetText("");
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        PauseController.SetPause(false);
        Action<int> callback = choiceCallback;
        Action closedCallback = dialogueClosedCallback;
        choiceCallback = null;
        dialogueClosedCallback = null;
        callback?.Invoke(-1);
        closedCallback?.Invoke();
    }
}