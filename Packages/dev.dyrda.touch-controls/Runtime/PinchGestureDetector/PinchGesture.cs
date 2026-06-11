using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/*
public class PinchGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private IPinchHandler needsUs = null; // todo switch to list: find all components on this and children.

    private int currentFirstFinger = -1;
    private int currentSecondFinger = -1;
    private int touchCount = 0;
    private bool pinching = false;

    private Vector2 positionFirst = Vector2.zero;
    private Vector2 positionSecond = Vector2.zero;
    private float previousDistance = 0f;
    private float delta = 0f;

    private void Awake()
    {
        needsUs = GetComponent(typeof(IPinchHandler)) as IPinchHandler;
    }

    public void OnPointerDown(PointerEventData data)
    {
        touchCount = touchCount + 1;

        if (currentFirstFinger == -1 && touchCount == 1)
        {
            // first finger must be a pure first finger and that's that

            currentFirstFinger = data.pointerId;
            positionFirst = data.position;

            return;
        }

        if (currentFirstFinger != -1 && currentSecondFinger == -1 && touchCount == 2)
        {
            // second finger must be a pure second finger and that's that

            currentSecondFinger = data.pointerId;
            positionSecond = data.position;

            FigureDelta();

            pinching = true;
            if (needsUs != null)
            {
                needsUs.OnPinchStart();
            }

            return;
        }
    }

    public void OnPointerUp(PointerEventData data)
    {
        touchCount = touchCount - 1;

        if (currentFirstFinger == data.pointerId)
        {
            currentFirstFinger = -1;

            if (pinching)
            {
                pinching = false;
                if (needsUs != null)
                {
                    needsUs.OnPinchEnd();
                }
            }
        }

        if (currentSecondFinger == data.pointerId)
        {
            currentSecondFinger = -1;

            if (pinching)
            {
                pinching = false;
                if (needsUs != null)
                {
                    needsUs.OnPinchEnd();
                }
            }
        }
    }

    public void OnDrag(PointerEventData data)
    {
        if (currentFirstFinger == data.pointerId)
        {
            positionFirst = data.position;
            FigureDelta();
        }

        if (currentSecondFinger == data.pointerId)
        {
            positionSecond = data.position;
            FigureDelta();
        }

        if (pinching)
        {
            if (data.pointerId == currentFirstFinger || data.pointerId == currentSecondFinger)
            {
                if (touchCount == 2)
                {
                    if (needsUs != null)
                    {
                        needsUs.OnPinchZoom(delta);
                    }
                }

                return;
            }
        }
    }

    private void DeterminePinch()
    {
        // Check touch #
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Get the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Get the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Get the difference in the distances between each frame.
            float deltaMagnitude = touchDeltaMag - prevTouchDeltaMag;

            // Resize the object
            this.transform.localScale += new Vector3(deltaMagnitude * scaleSpeed * Time.deltaTime, deltaMagnitude * scaleSpeed * Time.deltaTime, deltaMagnitude * scaleSpeed * Time.deltaTime);

            // Clamp the size to make sure it's between minSclae and maxScale. 
            this.transform.localScale = new Vector3(Mathf.Clamp(this.transform.localScale.x, minScale, maxScale),
                Mathf.Clamp(this.transform.localScale.y, minScale, maxScale),
                Mathf.Clamp(this.transform.localScale.z, minScale, maxScale));

      
        }
    }

    private void FigureDelta()
    {
        var newDistance = Vector2.Distance(positionFirst, positionSecond);
        delta = newDistance - previousDistance;
        previousDistance = newDistance;
    }

    struct PinchEventData
    {
        private float _magnitude;
        public float Magnitude => _magnitude;
        
        private float _deltaMagnitude;
        public float DeltaMagnitude => _deltaMagnitude;

        private Vector2 _centerPosition;
        public float CenterPosition => _centerPosition;

        public PinchEventData(float magnitude, float deltaMagnitude, Vector2 centerPosition)
        {
            _magnitude = magnitude;
            _deltaMagnitude = deltaMagnitude;
            _centerPosition = centerPosition;
        }
    }
}*/