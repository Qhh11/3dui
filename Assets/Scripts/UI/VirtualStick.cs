using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Serialization;

namespace DyrdaDev.TouchControls
{
    /// <summary>
    ///     A stick control displayed on screen for touch and other pointer
    ///     input.
    /// </summary>
    [AddComponentMenu("TouchControls/Virtual Stick")]
    public class VirtualStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Virtual Stick Components")]
        [SerializeField] private RectTransform socket;
        private Image socketImage;
        [SerializeField] private RectTransform knob;
        private Image knobImage;
        private RectTransform triggerArea;

        [Header("Stick Behavior")]
        [SerializeField] private StickBehavior _stickBehavior = StickBehavior.fixSocketAndCenterAsReference;
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
            get
            {
                return _mInternalValue;
            }
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
                    throw new Exception("You try to access the value of a virtual input stick. However, the stick is not set for this. Check the configuration of the stick in the Inspector.");
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
            movementRange = socketMagnitudeAxes == SocketMagnitudeAxes.width ? socket.sizeDelta.x / 2: socket.sizeDelta.y /2;
            UpdateVisibility(false);
        }

        void OnDisable()
        {
            ResetStick();
        }

        private void Update()
        {
            if (!GameplayInputLockService.IsBlocked)
            {
                return;
            }

            if (_mInternalValue != Vector2.zero ||
                knob.anchoredPosition != Vector2.zero)
            {
                ResetStick();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (GameplayInputLockService.IsBlocked)
            {
                ResetStick();
                return;
            }
            
            UpdateVisibility(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                triggerArea,
                eventData.position,
                eventData.pressEventCamera,
                out m_PointerDownPos);

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            if (GameplayInputLockService.IsBlocked)
            {
                ResetStick();
                return;
            }
            
            var referencePosition = _stickBehavior == StickBehavior.fixSocketAndCenterAsReference ? new Vector2(0,0) : m_PointerDownPos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                triggerArea,
                eventData.position,
                eventData.pressEventCamera,
                out var position);
            var delta = position - referencePosition;
            
            // Get normalized input position.
            delta /= movementRange;

            // Determine the correct input position relative to the center of the container independently of the pivot point  (rectTransform.pivot is normalized position)
            delta = new Vector2(delta.x + ((triggerArea.pivot.x - 0.5f) * 2 ),
                delta.y + ((triggerArea.pivot.y - 0.5f) * 2));

            delta = Vector2.ClampMagnitude(delta, 1.0f);

            if (_stickBehavior == StickBehavior.dynamicSocket)
            {
                socket.anchoredPosition = m_PointerDownPos;
            }
            
            // Update the position of the joystick image
            knob.anchoredPosition = new Vector2(delta.x * (socket.sizeDelta.x * 0.5f * maxStickOffset)
                , delta.y * (socket.sizeDelta.y) * 0.5f * maxStickOffset);
            
            InternalValue = delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetStick();
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            ResetStick();
        }

        private void ResetStick()
        {
            InternalValue = Vector2.zero;
            socket.anchoredPosition = m_SocketStartPos;
            knob.anchoredPosition = Vector2.zero;
            UpdateVisibility(false);
        }

        private void UpdateVisibility(bool active)
        {
            if (hideSocketOnInactive)
            {
                socketImage.enabled = active;
            }
            if (hideKnobOnInactive)
            {
                knobImage.enabled = active;
            }
        }

        public enum StickBehavior
        {
            fixSocketAndCenterAsReference,
            fixSocketAndPointerDownAsReference,
            dynamicSocket
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
    }
}
