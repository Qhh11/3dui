using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchRotate : MonoBehaviour {

    public float rotationFactor = 1.0f;  


	// Update is called once per frame
	void Update () {

        // Check touch #
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Get the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Get the direction of the vector between the touches in each frame.
            Vector2 prevTouchDeltaDir = (touchZeroPrevPos - touchOnePrevPos).normalized;
            Vector2 touchDeltaDir = (touchZero.position - touchOne.position).normalized;

            // Get the angular difference between each frame.
            var angle1 = Mathf.Atan2(touchDeltaDir.y, touchDeltaDir.x) * Mathf.Rad2Deg;
            var angle2 = Mathf.Atan2(prevTouchDeltaDir.y, prevTouchDeltaDir.x) * Mathf.Rad2Deg;

            Debug.Log(" angle: " + (angle1 - angle2));

            float deltaAngle = (angle1 - angle2);

            // Rotate the object
            this.transform.Rotate(transform.forward, deltaAngle * rotationFactor );

      
        }
    }
}
