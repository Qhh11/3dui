using UnityEngine;

public class CompassBarElement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private bool useFixDirection =
        false;

    [SerializeField] private Vector3 fixDirection;

    private CompassBar bar;
    private RectTransform rectTransform;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (player == null ||
            bar == null ||
            rectTransform == null ||
            (!useFixDirection &&
             target == null))
        {
            return;
        }

        Vector3 direction =
            useFixDirection
                ? fixDirection
                : target.position -
                  player.position;

        float angle =
            Vector2.SignedAngle(
                new Vector2(
                    player.forward.x,
                    player.forward.z
                ),
                new Vector2(
                    direction.x,
                    direction.z
                )
            );

        float mappedAngle =
            angle / 180f * -1f;

        float xPosition =
            mappedAngle *
            (360f / bar.BarRange) *
            (
                bar.BarRectTransform
                    .rect.width / 2f
            );

        rectTransform.anchoredPosition =
            new Vector2(
                xPosition,
                rectTransform
                    .anchoredPosition.y
            );
    }

    public void SetTarget(
        Transform newTarget)
    {
        target = newTarget;
        useFixDirection = false;

        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        if (bar == null)
        {
            bar =
                GetComponentInParent
                    <CompassBar>();
        }

        if (player == null &&
            bar != null)
        {
            player =
                bar.PlayerTransform;
        }
    }
}