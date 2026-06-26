using System;
using UnityEngine;
using UnityEngine.UI;

public class MarkController : MonoBehaviour
{
    [Serializable]
    private class MarkSlot
    {
        public Color color = Color.white;
        public MarkInstance mark;

        [NonSerialized]
        public CompassBarElement compassElement;
    }

    [Header("References")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Button markButton;
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private RocketLauncher rocketLauncher;

    [Header("Slots")]
    [SerializeField] private MarkSlot[] slots =
        new MarkSlot[3];

    [Header("Raycast")]
    [SerializeField] private float maxAimDistance =
        50000f;

    [SerializeField] private LayerMask placementMask =
        ~0;

    [SerializeField] private LayerMask recallMask;

    [SerializeField] private float surfaceOffset =
        0.1f;

    [Header("Movement")]
    [SerializeField] private float launchSpeed =
        3000f;

    [SerializeField] private float returnSpeed =
        3000f;

    [Header("Visual")]
    [SerializeField]
    private float visualTransitionDuration = 0.08f;

    [SerializeField] private float aimedScale =
        1.12f;

    [Header("Compass")]
    [SerializeField]
    private CompassBarElement compassElementPrefab;

    [SerializeField]
    private RectTransform compassElementParent;

    [SerializeField]
    private Material compassSoftMaskMaterial;

    private int selectedIndex = -1;
    private int lastActionFrame = -1;

    private MarkInstance hoveredMark;
    private RaycastHit placementHit;
    private bool hasPlacementHit;

    public bool ActedThisFrame =>
        lastActionFrame == Time.frameCount;

    public bool IsBusy
    {
        get
        {
            if (slots == null)
                return false;

            foreach (MarkSlot slot in slots)
            {
                if (slot?.mark == null)
                    continue;

                if (slot.mark.State ==
                        MarkInstance.MarkState.Launching ||
                    slot.mark.State ==
                        MarkInstance.MarkState.Returning)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        if (slots == null)
            return;

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            MarkSlot slot = slots[i];

            if (slot?.mark == null)
                continue;

            slot.compassElement = null;

            slot.mark.Bind(
                this,
                i,
                slot.color,
                visualTransitionDuration,
                aimedScale
            );

            slot.mark.SetSelected(false);
        }

        SelectFirstAvailable();
    }

    private void Update()
    {
        RefreshSelectorStates();
        RefreshCompassVisibility();
        RefreshAimAndButton();
    }

    public void OnMarkButtonClicked()
    {
        RefreshAimAndButton();

        if (!CanOperateMark())
            return;

        if (hoveredMark != null &&
            hoveredMark.IsDeployed)
        {
            BeginRecall(hoveredMark);
            return;
        }

        if (slots == null ||
            selectedIndex < 0 ||
            selectedIndex >= slots.Length ||
            !hasPlacementHit)
        {
            return;
        }

        BeginLaunch(
            selectedIndex,
            placementHit
        );
    }

    public void SelectSlot(int index)
    {
        if (!CanUseSelection())
            return;

        if (slots == null ||
            index < 0 ||
            index >= slots.Length)
        {
            return;
        }

        MarkInstance mark =
            slots[index]?.mark;

        if (mark == null ||
            mark.State !=
                MarkInstance.MarkState.Selector)
        {
            return;
        }

        SetSelectedIndex(index);
    }

    public void NotifyDeploymentCompleted(
        int index,
        MarkInstance mark)
    {
        if (!IsValidSlotMark(index, mark))
            return;

        CreateCompassElement(index);
    }

    public void NotifyReturnCompleted(
        int index,
        MarkInstance mark)
    {
        if (!IsValidSlotMark(index, mark))
            return;

        DestroyCompassElement(
            slots[index]
        );

        if (selectedIndex < 0)
        {
            SetSelectedIndex(index);
        }
        else
        {
            mark.SetSelected(false);
        }

        RefreshSelectorStates();
    }

    private void BeginLaunch(
        int index,
        RaycastHit hit)
    {
        MarkSlot slot = slots[index];

        if (slot?.mark == null ||
            slot.mark.State !=
                MarkInstance.MarkState.Selector)
        {
            return;
        }

        Vector3 targetPosition =
            hit.point +
            hit.normal * surfaceOffset;

        bool started =
            slot.mark.Launch(
                targetPosition,
                hit.collider != null
                    ? hit.collider.transform
                    : null,
                launchSpeed,
                returnSpeed
            );

        if (!started)
            return;

        lastActionFrame = Time.frameCount;

        slot.mark.SetSelected(false);

        SelectNextAvailable(index);
    }

    private void BeginRecall(
        MarkInstance mark)
    {
        int index = FindMarkIndex(mark);

        if (index < 0)
            return;

        if (!mark.BeginReturn())
            return;

        lastActionFrame = Time.frameCount;

        ClearHoveredMark();

        DestroyCompassElement(
            slots[index]
        );
    }

    private void RefreshSelectorStates()
    {
        bool gameplayInputBlocked =
            GameplayInputLockService.IsBlocked;

        bool selectorVisible =
            IsUfoView() &&
            !gameplayInputBlocked;

        bool selectorInputEnabled =
            selectorVisible &&
            !IsBusy &&
            (siphonController == null ||
             !siphonController.IsBusy) &&
            (rocketLauncher == null ||
             !rocketLauncher.FiredThisFrame);

        if (slots == null)
            return;

        foreach (MarkSlot slot in slots)
        {
            if (slot?.mark == null)
                continue;

            slot.mark.SetSelectorPresentation(
                selectorVisible,
                selectorInputEnabled
            );
        }
    }

    private void RefreshAimAndButton()
    {
        ClearHoveredMark();
        hasPlacementHit = false;

        if (!CanOperateMark())
        {
            SetMarkButtonInteractable(false);
            return;
        }

        Camera targetCamera = GetAimCamera();

        if (targetCamera == null)
        {
            SetMarkButtonInteractable(false);
            return;
        }

        Ray ray =
            targetCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );

        if (Physics.Raycast(
                ray,
                out RaycastHit recallHit,
                maxAimDistance,
                recallMask,
                QueryTriggerInteraction.Collide))
        {
            MarkInstance targetMark =
                recallHit.collider
                    .GetComponentInParent
                        <MarkInstance>();

            if (targetMark != null &&
                targetMark.IsDeployed &&
                FindMarkIndex(targetMark) >= 0)
            {
                hoveredMark = targetMark;
                hoveredMark.SetAimed(true);

                SetMarkButtonInteractable(true);
                return;
            }
        }

        hasPlacementHit =
            Physics.Raycast(
                ray,
                out placementHit,
                maxAimDistance,
                placementMask,
                QueryTriggerInteraction.Ignore
            );

        bool canLaunch =
            slots != null &&
            selectedIndex >= 0 &&
            selectedIndex < slots.Length &&
            slots[selectedIndex]?.mark != null &&
            slots[selectedIndex].mark.State ==
                MarkInstance.MarkState.Selector &&
            hasPlacementHit;

        SetMarkButtonInteractable(canLaunch);
    }

    private bool CanOperateMark()
    {
        if (!IsUfoView())
            return false;

        if (GameplayInputLockService.IsBlocked)
        {
            return false;
        }

        if (siphonController != null &&
            siphonController.IsBusy)
        {
            return false;
        }

        if (rocketLauncher != null &&
            rocketLauncher.FiredThisFrame)
        {
            return false;
        }

        return !IsBusy;
    }

    private bool CanUseSelection()
    {
        return CanOperateMark();
    }

    private bool IsUfoView()
    {
        if (viewToggleButton != null)
            return viewToggleButton.IsUfoView();

        return
            SwitchPerspectiveButtonController.Instance
                != null &&
            SwitchPerspectiveButtonController.Instance
                .IsUfoView;
    }

    private Camera GetAimCamera()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        return aimCamera;
    }

    private void ClearHoveredMark()
    {
        if (hoveredMark != null)
            hoveredMark.SetAimed(false);

        hoveredMark = null;
    }

    private void SetSelectedIndex(int index)
    {
        if (slots != null)
        {
            for (int i = 0;
                 i < slots.Length;
                 i++)
            {
                if (slots[i]?.mark != null)
                {
                    slots[i].mark.SetSelected(
                        i == index
                    );
                }
            }
        }

        selectedIndex = index;
    }

    private void SelectFirstAvailable()
    {
        if (slots != null)
        {
            for (int i = 0;
                 i < slots.Length;
                 i++)
            {
                if (slots[i]?.mark != null &&
                    slots[i].mark.State ==
                        MarkInstance
                            .MarkState.Selector)
                {
                    SetSelectedIndex(i);
                    return;
                }
            }
        }

        SetSelectedIndex(-1);
    }

    private void SelectNextAvailable(
        int previousIndex)
    {
        if (slots == null ||
            slots.Length == 0)
        {
            SetSelectedIndex(-1);
            return;
        }

        for (int offset = 1;
             offset <= slots.Length;
             offset++)
        {
            int index =
                (previousIndex + offset) %
                slots.Length;

            if (slots[index]?.mark != null &&
                slots[index].mark.State ==
                    MarkInstance.MarkState.Selector)
            {
                SetSelectedIndex(index);
                return;
            }
        }

        SetSelectedIndex(-1);
    }

    private int FindMarkIndex(
        MarkInstance mark)
    {
        if (slots == null || mark == null)
            return -1;

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            if (slots[i]?.mark == mark)
                return i;
        }

        return -1;
    }

    private bool IsValidSlotMark(
        int index,
        MarkInstance mark)
    {
        return
            slots != null &&
            index >= 0 &&
            index < slots.Length &&
            slots[index]?.mark == mark;
    }

    private void CreateCompassElement(
        int index)
    {
        if (compassElementPrefab == null ||
            compassElementParent == null ||
            slots[index]?.mark == null)
        {
            return;
        }

        MarkSlot slot = slots[index];

        DestroyCompassElement(slot);

        slot.compassElement =
            Instantiate(
                compassElementPrefab,
                compassElementParent
            );

        slot.compassElement.SetTarget(
            slot.mark.transform
        );

        Image image =
            slot.compassElement
                .GetComponentInChildren
                    <Image>(true);

        if (image != null)
        {
            image.color = slot.color;
            image.raycastTarget = false;

            if (compassSoftMaskMaterial != null)
            {
                image.material =
                    compassSoftMaskMaterial;
            }
        }

        slot.compassElement
            .gameObject.SetActive(
                viewToggleButton != null &&
                viewToggleButton.IsPlayerView()
            );
    }

    private void DestroyCompassElement(
        MarkSlot slot)
    {
        if (slot == null ||
            slot.compassElement == null)
        {
            return;
        }

        Destroy(
            slot.compassElement.gameObject
        );

        slot.compassElement = null;
    }

    private void RefreshCompassVisibility()
    {
        bool visible =
            viewToggleButton != null &&
            viewToggleButton.IsPlayerView();

        if (slots == null)
            return;

        foreach (MarkSlot slot in slots)
        {
            if (slot?.compassElement == null ||
                slot.mark == null)
            {
                continue;
            }

            slot.compassElement
                .gameObject.SetActive(
                    visible &&
                    slot.mark.State ==
                        MarkInstance
                            .MarkState.Deployed
                );
        }
    }

    private void SetMarkButtonInteractable(
        bool value)
    {
        if (markButton != null)
            markButton.interactable = value;
    }
}
