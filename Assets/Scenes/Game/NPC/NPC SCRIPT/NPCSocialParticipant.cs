using UnityEngine;

public class NPCSocialParticipant : MonoBehaviour
{
    [Header("Participation")]
    [SerializeField] private bool canSocialize = true;
    [SerializeField] private string displayName;
    [SerializeField] private Transform speechAnchor;
    [SerializeField] private WaypointMover waypointMover;

    [Header("Conversation Lines")]
    [SerializeField] private string calmLine = "It feels peaceful today.";
    [SerializeField] private string concernedLine = "We should take care of the house.";
    [SerializeField] private string angryLine = "Something needs to change.";

    public bool CanSocialize => canSocialize && waypointMover != null;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Transform SpeechAnchor => speechAnchor != null ? speechAnchor : transform;
    public WaypointMover WaypointMover => waypointMover;

    private void Awake()
    {
        if (waypointMover == null)
            waypointMover = GetComponent<WaypointMover>();
    }

    public string GetLine(HouseholdMood mood)
    {
        switch (mood)
        {
            case HouseholdMood.Calm:
                return calmLine;
            case HouseholdMood.Angry:
                return angryLine;
            default:
                return concernedLine;
        }
    }
}
