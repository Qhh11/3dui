using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Serialization;

namespace DyrdaIo.TouchControls
{
    /// <summary>
    ///     A stick control displayed on screen and moved around by touch or other pointer
    ///     input.
    /// </summary>
    [AddComponentMenu("TouchControls/Virtual Stick")]
    public class VirtualStick : OnScreenControl, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Virtual Stick Components")]
        [SerializeField] private RectTransform socket;
        private Image socketImage;
        [SerializeField] private RectTransform knob;
        private Image knobImage;
        private RectTransform triggerArea;

        [Header("Stick Behavior")]
        [SerializeField] private bool hideSocketOnInactive = false;
        [SerializeField] private bool hideKnobOnInactive = false;
        [SerializeField] private SocketMagnitudeAxes socketMagnitudeAxes = SocketMagnitudeAxes.width;
        private float movementRange = 1;
        [SerializeField] private float maxStickOffset = 0.7f;

        [Header("Input System Configuration")]
        [SerializeField] private InputSystemType inputSystemType;
        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private Vector2 m_PointerDownPos;
        private Vector2 m_SocketStartPos;

        private Vector2 _mInternalValue;

        public Vector2 InternalValue
        {
            get => _mInternalValue;
            private set
            {
                _mInternalValue = value;

                if (inputSystemType == InputSystemType.inputSystemPackage || inputSystemType == InputSystemType.both)
                {
                    SendValueToControl(value);
                }
            }
        }

        public Vector2 Value
        {
            get
            {
                if (inputSystemType == InputSystemType.publicProperty || inputSystemType == InputSystemType.both)
                {
                    return _mInternalValue;
                }
                else
                {
                    throw new Exception(
                        "You try to access the value of a virtual input stick. However, the stick is not set for this. Check the configuration of the stick in the Inspector.");
                }
            }
        }

        private void Start()
        {
            InternalValue = Vector2.zero;
            m_SocketStartPos = socket.anchoredPosition;
            triggerArea = GetComponent<RectTransform>();
            socketImage = socket.GetComponent<Image>();
            knobImage = knob.GetComponent<Image>();
            movementRange = socketMagnitudeAxes == SocketMagnitudeAxes.width
                ? socket.sizeDelta.x / 2
                : socket.sizeDelta.y / 2;
        }


        public enum InputSystemType
        {
            inputSystemPackage,
            publicProperty,
            both
        }

        public enum SocketMagnitudeAxes
        {
            width,
            height
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(triggerArea,
                eventData.position, eventData.pressEventCamera
                , out var position);
            var delta = position;
            //delta.Normalize();
            delta = new Vector2(delta.x + (triggerArea.pivot.x - 0.5f) * 2
                , delta.y + (triggerArea.pivot.y - 0.5f) * 2);
            
            delta = delta.normalized * Mathf.Clamp(delta.magnitude / (socket.sizeDelta.x * 0.5f), 0, 1);
            Debug.Log(delta.magnitude);
            InternalValue = delta;
            knob.anchoredPosition = new Vector2(delta.x * (socket.sizeDelta.x * 0.5f * maxStickOffset)
                , delta.y * (socket.sizeDelta.y * 0.5f * maxStickOffset));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            InternalValue = Vector2.zero;
            socket.anchoredPosition = m_SocketStartPos;
            knob.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }
    }
}