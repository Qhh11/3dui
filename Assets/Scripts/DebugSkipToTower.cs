using UnityEngine;

public class DebugSkipToTower : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (TowerFlowController.Instance == null)
            {
                Debug.LogError("[DEBUG] Instance is NULL!");
                return;
            }

            // 关掉可能正在播的对话，避免冲突
            //DialogueRunner.Instance?.ForceEndDialogue();

            TowerFlowController.Instance.DebugForceStateToWaitingForBlast();
            Debug.Log("[DEBUG] Forced state via T key. Rocket should be unlocked now.");
        }
    }
}