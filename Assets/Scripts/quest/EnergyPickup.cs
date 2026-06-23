using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnergyPickup : MonoBehaviour, IInteractable
{
    [Header("Dialogue after pickup")]
    [SerializeField] private string dialogueSequenceId = "well_energy_pickup";

    [Header("Objects")]
    [SerializeField] private GameObject energyGlow;
    [SerializeField] private GameObject energyParticles;
    [SerializeField] private GameObject outlineEffect;

    [Header("Bush split")]
    [SerializeField] private GameObject bigBush;
    [SerializeField] private GameObject leftBush;
    [SerializeField] private GameObject rightBush;
    [SerializeField] private float splitDistance = 150f;
    [SerializeField] private float splitDuration = 0.8f;

    [Header("Gameplay")]
    [SerializeField] private UnityEvent onPickedUp;

    private bool pickedUp = false;

    public void Interact()
    {
        if (pickedUp) return;
        pickedUp = true;

        StartCoroutine(PickupSequence());
    }

    private IEnumerator PickupSequence()
    {
        // Step 1: Hide outline immediately
        if (outlineEffect != null)
            outlineEffect.SetActive(false);
        // Step 1: Big bush disappears
        if (bigBush != null)
            bigBush.SetActive(false);



        // Step 2: Two small bushes appear at big bush's position
        Vector3 center = bigBush != null 
            ? bigBush.transform.position 
            : transform.position;

        if (leftBush != null)
        {
            leftBush.transform.position = center;
            leftBush.SetActive(true);
        }
        if (rightBush != null)
        {
            rightBush.transform.position = center;
            rightBush.SetActive(true);
        }

        // Step 3: Animate small bushes sliding apart
        Vector3 leftTarget = center + Vector3.left * splitDistance;
        Vector3 rightTarget = center + Vector3.right * splitDistance;
        float elapsed = 0f;

        while (elapsed < splitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / splitDuration);

            if (leftBush != null)
                leftBush.transform.position = Vector3.Lerp(
                    center, leftTarget, t);
            if (rightBush != null)
                rightBush.transform.position = Vector3.Lerp(
                    center, rightTarget, t);

            yield return null;
        }

        // Step 4: Wait a moment, let player see the energy ball
        yield return new WaitForSeconds(0.5f);
        // Hide particles
        if (energyParticles != null)
            energyParticles.SetActive(false);


        // Hide renderers instead of disabling gameObject
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;

        // Also disable collider so player can't click again
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Step 6: Play pickup dialogue
        if (Level1DialogueDatabase.Instance != null)
        {
            DialogueSequence seq = Level1DialogueDatabase.Instance
                .GetSequence(dialogueSequenceId);
            if (seq != null && DialogueRunner.Instance != null)
                DialogueRunner.Instance.StartDialogue(seq);
        }

        // Step 6: Wait for dialogue to finish
        while (DialogueRunner.Instance != null 
            && DialogueRunner.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        // Step 7: Gameplay hook
        onPickedUp?.Invoke();

        // Step 8: Hide self
        gameObject.SetActive(false);
    }
}