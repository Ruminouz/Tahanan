using UnityEngine;

[CreateAssetMenu(fileName = "New NPC Dialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
  public string npcName;
  public Sprite npcPortrait;
  public string[] dialogueLines;
  public bool[] autoProgressLines;
  public float autoProgressDelay = 1.5f;
  public float typingSpeed = 0.05f;
  public AudioClip voiceSound;
  public float voicePitch = 1.0f;

}
