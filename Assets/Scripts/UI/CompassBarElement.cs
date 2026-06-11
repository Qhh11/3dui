using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CompassBarElement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private bool useFixDirection = false;
    [SerializeField] private Vector3 fixDirection;

    private CompassBar bar;
    private RectTransform _rectTransform;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        bar = GetComponentInParent<CompassBar>();
    }

    private void Update()
    {
        var direction = useFixDirection ? fixDirection : target.transform.position - player.transform.position;
        var angle = Vector2.SignedAngle(new Vector2(player.forward.x, player.forward.z),
            new Vector2(direction.x, direction.z));
        var mappedAngle = (float) (angle / 180) * -1;
        var xPosition = mappedAngle * (360 / bar.BarRange) * (bar.BarRectTransform.rect.width / 2);
        _rectTransform.anchoredPosition = new Vector2(xPosition, 0);
    }
}