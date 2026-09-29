using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class LevelFlow : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform startSpawn;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private float fallHeight = -8f;

    private bool completed;

    private void Start()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (completed)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (player != null && player.transform.position.y < fallHeight)
        {
            Respawn();
        }
    }

    public void Configure(PlayerMovement controlledPlayer, Transform spawn, GameObject finishPanel, float resetHeight)
    {
        player = controlledPlayer;
        startSpawn = spawn;
        completionPanel = finishPanel;
        fallHeight = resetHeight;
    }

    public void Respawn()
    {
        if (player != null && startSpawn != null)
        {
            player.TeleportTo(startSpawn.position, startSpawn.rotation);
        }
    }

    public void Complete()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        player.SetMovementEnabled(false);
        completionPanel.SetActive(true);
    }
}
