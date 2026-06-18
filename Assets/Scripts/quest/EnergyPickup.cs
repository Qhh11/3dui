using UnityEngine;
using UnityEngine.Events;

public class EnergyPickup : MonoBehaviour, IInteractable
{
    [Header("Dialogue after pickup")]
    [SerializeField] private string dialogueSequenceId = "well_energy_pickup";

    [Header("Objects to hide after pickup")]
    [SerializeField] private GameObject energyGlow;      // 发光球
    [SerializeField] private GameObject energyParticles;  // 粒子
    [SerializeField] private GameObject bushes;           // 灌木

    [Header("Gameplay hook — teammates fill this in")]
    [SerializeField] private UnityEvent onPickedUp;
    // 组员在Inspector里绑：解锁虹吸、加能源块计数、播特效等

    private bool pickedUp = false;

    public void Interact()
    {
        if (pickedUp) return;
        pickedUp = true;

        // 隐藏发光球和粒子
        if (energyGlow != null)
            energyGlow.SetActive(false);
        if (energyParticles != null)
            energyParticles.SetActive(false);

        // 触发拾取后的对话（UFO终端报告解锁虹吸）
        if (Level1DialogueDatabase.Instance != null)
        {
            DialogueSequence seq = Level1DialogueDatabase.Instance
                .GetSequence(dialogueSequenceId);
            if (seq != null)
                DialogueRunner.Instance.StartDialogue(seq);
        }

        // 调用组员的gameplay逻辑
        onPickedUp?.Invoke();

        // 隐藏自己（拾取完就没了）
        gameObject.SetActive(false);
    }
}