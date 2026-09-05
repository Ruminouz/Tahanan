using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        if (dialogueData == null || (PauseController.IsGamePaused && !isDialogueActive)) 
            return;

        if (!isDialogueActive)
        {
            StartDialogue();
        }
        else
        {
            NextLine();
        }
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcPortrait;
        dialoguePanel.SetActive(true);
        PauseController.SetPause(true);

        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            SoundEffectManager.StopVoice(); // Stop audio if player skips typing
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
        }
        else if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
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

    foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
    {
        dialogueText.text += letter;

        if (letter != ' ' && dialogueData.voiceSound != null)
        {
            SoundEffectManager.PlayVoice(dialogueData.voiceSound, dialogueData.voicePitch);
        }

        yield return new WaitForSeconds(dialogueData.typingSpeed);
    }

    isTyping = false;

    if (dialogueData.autoProgressLines.Length > dialogueIndex && dialogueData.autoProgressLines[dialogueIndex])
    {
        yield return new WaitForSeconds(dialogueData.autoProgressDelay);
        NextLine();
    }
}


    public void EndDialogue()
    {
        StopAllCoroutines();
        SoundEffectManager.StopVoice();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);
        PauseController.SetPause(false);
    }
}