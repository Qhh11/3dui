using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IPinchHandler
{
    void OnPinchStart ();
    void OnPinchEnd ();
    void OnPinchZoom (float gapDelta);
}