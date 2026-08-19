using UnityEngine;

namespace HouseChoresGame
{
    [CreateAssetMenu(fileName = "NewChore", menuName = "Chores/Chore Data")]
    public class ChoreData : ScriptableObject
    {
        [Header("Chore Info")]
        public string choreName;
        public float timeLimit = 30f;

        [Header("UI Prefab")]
        public GameObject choreUIPrefab;
    }
}
