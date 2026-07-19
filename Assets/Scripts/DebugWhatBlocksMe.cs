using UnityEngine;

public class DebugWhatBlocksMe : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // CharacterController 撞到东西时打印
        Debug.Log($"[BLOCK] Hit: {hit.gameObject.name} | Layer: {LayerMask.LayerToName(hit.gameObject.layer)} | Path: {GetPath(hit.transform)}");
    }

    private void OnCollisionStay(Collision collision)
    {
        // Rigidbody 方式移动时撞到东西打印
        Debug.Log($"[BLOCK] Colliding with: {collision.gameObject.name} | Path: {GetPath(collision.transform)}");
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}