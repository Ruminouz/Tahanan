using UnityEngine;
using UnityEngine.UI;

public class TrashItem : MonoBehaviour
{
    [SerializeField] private ThrowTrashMiniGame miniGame;
    [SerializeField] private Button button;

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(Throw);
        }
    }

    private void Throw()
    {
        if (miniGame != null)
        {
            miniGame.ThrowTrash(gameObject);
        }
    }
}