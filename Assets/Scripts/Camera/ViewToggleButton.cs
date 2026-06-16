using UnityEngine;
using Cinemachine;
using TMPro;

public class ViewToggleButton : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera firstPersonCam;
    public CinemachineVirtualCamera secondaryCam;

    [Header("Camera Controllers")]
    public MonoBehaviour firstPersonController;
    public MonoBehaviour secondaryController;

    [Header("View Object Roots")]
    public GameObject[] playerViewObjects;
    public GameObject[] ufoViewObjects;

    [Header("UI")]
    public TextMeshProUGUI switchLabel;

    [Header("Priorities")]
    public int activePriority = 20;
    public int standbyPriority = 10;

    [Header("Start State")]
    public bool startFirstPerson = true;

    [Header("Recenter")]
    public bool recenterOnActivate = true;

    [Header("Optional")]
    public TouchDragger touchDragger;

    private bool isFirstPerson;

    private void Start()
    {
        isFirstPerson = startFirstPerson;
        Apply();
    }

    public void Toggle()
    {
        isFirstPerson = !isFirstPerson;
        Debug.Log("Switch perspective clicked. Is first person: " + isFirstPerson);
        Apply();
    }

    public void SwitchToPlayer()
    {
        isFirstPerson = true;
        Debug.Log("Switch to Player perspective.");
        Apply();
    }

    public void SwitchToUfo()
    {
        isFirstPerson = false;
        Debug.Log("Switch to UFO perspective.");
        Apply();
    }

    public bool IsPlayerView()
    {
        return isFirstPerson;
    }

    public bool IsUfoView()
    {
        return !isFirstPerson;
    }

    private void Apply()
    {
        ApplyCameraPriority();
        ApplyCameraControllers();
        ApplyViewObjects();
        UpdateSwitchLabel();
    }

    private void ApplyCameraPriority()
    {
        if (firstPersonCam != null)
        {
            firstPersonCam.Priority = isFirstPerson ? activePriority : standbyPriority;
        }

        if (secondaryCam != null)
        {
            secondaryCam.Priority = isFirstPerson ? standbyPriority : activePriority;
        }
    }

    private void ApplyCameraControllers()
    {
        SetController(firstPersonController, isFirstPerson);
        SetController(secondaryController, !isFirstPerson);

        if (touchDragger != null)
        {
            touchDragger.enabled = isFirstPerson;
        }
    }

    private void ApplyViewObjects()
    {
        SetObjectsActive(playerViewObjects, isFirstPerson);
        SetObjectsActive(ufoViewObjects, !isFirstPerson);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target == null)
            {
                continue;
            }

            target.SetActive(active);
        }
    }

    private void SetController(MonoBehaviour controller, bool active)
    {
        if (controller == null)
        {
            return;
        }

        controller.enabled = active;

        if (active && recenterOnActivate)
        {
            controller.SendMessage("Recenter", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void UpdateSwitchLabel()
    {
        if (switchLabel == null)
        {
            return;
        }

        switchLabel.text = isFirstPerson ? "Switch to UFO" : "Switch to Player";
    }
}