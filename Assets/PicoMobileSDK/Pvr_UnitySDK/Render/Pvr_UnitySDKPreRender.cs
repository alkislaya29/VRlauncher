using UnityEngine;


[RequireComponent(typeof(Camera))]
public class Pvr_UnitySDKPreRender : MonoBehaviour
{
    public Camera cam { get; private set; }

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Reset()
    {
#if UNITY_EDITOR
        var cam = GetComponent<Camera>();
#endif

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 0;
        cam.useOcclusionCulling = false;
        cam.depth = -100;
    }
}
