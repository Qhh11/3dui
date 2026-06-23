using UnityEngine;

public class TouchInteraction : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTouch(Input.GetTouch(0).position);
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleTouch(Input.mousePosition);
        }
    }

    void HandleTouch(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50000f, ~0, QueryTriggerInteraction.Ignore);


        // Find the first hit that has IInteractable
        foreach (RaycastHit hit in hits)
        {
            IInteractable interactable = hit.collider
                .GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }
    }
}