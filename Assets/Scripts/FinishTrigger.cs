using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class FinishTrigger : MonoBehaviour
{
    [SerializeField] private LevelFlow levelFlow;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            levelFlow.Complete();
        }
    }

    public void Configure(LevelFlow flow)
    {
        levelFlow = flow;
    }
}
