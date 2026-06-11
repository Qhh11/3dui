using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchScale : MonoBehaviour {

    public float scaleSpeed = 0.5f;
    public float minScale = 0.5f;
    public float maxScale = 3.0f;
   

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
}
