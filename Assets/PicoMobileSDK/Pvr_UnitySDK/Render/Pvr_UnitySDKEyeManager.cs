using UnityEngine;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

public class Pvr_UnitySDKEyeManager : MonoBehaviour
{
    public bool isfirst = true;
	private int framenum = 0;

    private int RenderLayersMax = 4;

    private int CenterIdindex = 0;
    /************************************    Properties  *************************************/
    #region Properties
    private Pvr_UnitySDKEye[] eyes = null;
    public Pvr_UnitySDKEye[] Eyes
    {
        get
        {
            if (eyes == null)
            {
                eyes = GetComponentsInChildren<Pvr_UnitySDKEye>(true).ToArray();
            }
            return eyes;
        }
    }

    // StandTexture Overlay
    private Pvr_UnitySDKEyeOverlay[] overlays = null;
    public Pvr_UnitySDKEyeOverlay[] Overlays
    {
        get
        {
            if (overlays == null)
            {
                overlays = Pvr_UnitySDKEyeOverlay.Instances.ToArray();
            }
            return overlays;
        }
    }



    public Camera ControllerCamera
    {
        get
        {
            return GetComponent<Camera>();
        }
    }

    private bool renderedStereo = false;

    private int ScreenHeight
    {
        get
        {
            return Screen.height - (Application.isEditor ? 36 : 0);
        }
    }

    #endregion

    /************************************ Process Interface  *********************************/
    #region  Process Interface
    public void AddStereoRig()
    {
        if (Eyes.Length > 0)
        {
            return;
        }
        CreateEye(Pvr_UnitySDKAPI.Eye.LeftEye);
        CreateEye(Pvr_UnitySDKAPI.Eye.RightEye);
    }

    private void CreateEye(Pvr_UnitySDKAPI.Eye eyeSide)
    {
        string nm = name + (eyeSide == Pvr_UnitySDKAPI.Eye.LeftEye ? " LeftEye" : " RightEye");
        GameObject go = new GameObject(nm);
        go.transform.parent = transform;
        go.AddComponent<Camera>().enabled = true;

        var picovrEye = go.AddComponent<Pvr_UnitySDKEye>();
        picovrEye.eyeSide = eyeSide;
    }

    private void FillScreenRect(int width, int height, Color color)
    {
        int x = Screen.width / 2;
        int y = Screen.height / 2 - 15;
        width /= 2;
        height /= 2;
        Pvr_UnitySDKManager.SDK.Middlematerial.color = color;
        Pvr_UnitySDKManager.SDK.Middlematerial.SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();
        GL.Color(Color.white);
        GL.Begin(GL.QUADS);
        GL.Vertex3(x - width, y - height, 0);
        GL.Vertex3(x - width, y + height, 0);
        GL.Vertex3(x + width, y + height, 0);
        GL.Vertex3(x + width, y - height, 0);
        GL.End();
        GL.PopMatrix();

    }

    private void SetupCenterCamera()
    {
        transform.localPosition = Vector3.zero;
        ControllerCamera.aspect = 1.0f;
        ControllerCamera.rect = new Rect(0, 0, 1, 1);
    }

    private void SetupUpdate()
    {
        ControllerCamera.fieldOfView = Pvr_UnitySDKManager.SDK.EyeFov;
        CenterIdindex = Pvr_UnitySDKManager.SDK.currEyeTextureIdx;
        ControllerCamera.enabled = true;
    }

    private void CenterEyeRender()
    {
        SetupUpdate();
        if (Pvr_UnitySDKManager.SDK.eyeTextures[CenterIdindex] != null)
        {
            Pvr_UnitySDKManager.SDK.eyeTextures[CenterIdindex].DiscardContents();
            ControllerCamera.targetTexture = Pvr_UnitySDKManager.SDK.eyeTextures[CenterIdindex];
        }
    }
    #endregion

    /*************************************  Unity API ****************************************/
    #region Unity API
    void Awake()
    {
        //AddStereoRig();
    }
    void Start()
    {
#if !UNITY_EDITOR
        SetupCenterCamera();
        ControllerCamera.enabled = Pvr_UnitySDKManager.SDK.Monoscopic;
#endif
    }
    void OnEnable()
    {
        StartCoroutine("EndOfFrame");
    }

    void Update()
    {

        ControllerCamera.enabled = !Pvr_UnitySDKManager.SDK.VRModeEnabled || Pvr_UnitySDKManager.SDK.Monoscopic;
        
#if UNITY_EDITOR
        for (int i = 0; i < Eyes.Length; i++)
        {
            Eyes[i].eyecamera.enabled = Pvr_UnitySDKManager.SDK.VRModeEnabled;
        }
#else
        for (int i = 0; i < Eyes.Length; i++)
        {
            Eyes[i].eyecamera.enabled = !Pvr_UnitySDKManager.SDK.Monoscopic;
        }
#endif

        if (!Pvr_UnitySDKManager.SDK.IsViewerLogicFlow)
        {
            if (!Pvr_UnitySDKManager.SDK.Monoscopic)
            {
                for (int i = 0; i < Eyes.Length; i++)
                {
                    Eyes[i].EyeRender();
                }
            }
            else
            {
                CenterEyeRender();
            }
            
        }
    }
    void OnDisable()
    {
        StopAllCoroutines();
    }

    void OnPostRender()
    {
        int eyeTextureID = Pvr_UnitySDKManager.SDK.eyeTextureIds[CenterIdindex];
        Pvr_UnitySDKPluginEvent.IssueWithData(RenderEventType.LeftEyeEndFrame, eyeTextureID);
        Pvr_UnitySDKPluginEvent.IssueWithData(RenderEventType.RightEyeEndFrame, eyeTextureID);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (Pvr_UnitySDKEyeOverlay.Instances.Count <= 0)
        {
            return;
        }
        Vector4 clipLowerLeft = new Vector4(-1, -1, 0, 1);
        Vector4 clipUpperRight = new Vector4(1, 1, 0, 1);

        Pvr_UnitySDKEyeOverlay.Instances.Sort();
        foreach (var eyeOverlay in Pvr_UnitySDKEyeOverlay.Instances)
        {
            if (!eyeOverlay.isActiveAndEnabled) continue;
            if (eyeOverlay.imageTexture == null) continue;
            if (eyeOverlay.imageTransform != null && !eyeOverlay.imageTransform.gameObject.activeSelf) continue;
            if (eyeOverlay.imageTransform != null && !eyeOverlay.imageTransform.IsChildOf(this.transform.parent)) continue;

            Rect textureRect = new Rect(0, 0, 1, 1);

            Vector2 leftCenter = new Vector2(Screen.width * 0.25f, Screen.height * 0.5f);
            Vector2 rightCenter = new Vector2(Screen.width * 0.75f, Screen.height * 0.5f);
            Vector2 eyeExtent = new Vector3(Screen.width * 0.25f, Screen.height * 0.5f);
            eyeExtent.x -= 100.0f;
            eyeExtent.y -= 100.0f;

            Rect leftScreen = Rect.MinMaxRect(
                leftCenter.x - eyeExtent.x,
                leftCenter.y - eyeExtent.y,
                leftCenter.x + eyeExtent.x,
                leftCenter.y + eyeExtent.y);
            Rect rightScreen = Rect.MinMaxRect(
                rightCenter.x - eyeExtent.x,
                rightCenter.y - eyeExtent.y,
                rightCenter.x + eyeExtent.x,
                rightCenter.y + eyeExtent.y);

            var eyeRectMin = clipLowerLeft; eyeRectMin /= eyeRectMin.w;
            var eyeRectMax = clipUpperRight; eyeRectMax /= eyeRectMax.w;

            if (eyeOverlay.eyeSide == Pvr_UnitySDKAPI.Eye.LeftEye)
            {
                leftScreen = Rect.MinMaxRect(
                        leftCenter.x + eyeExtent.x * eyeRectMin.x,
                        leftCenter.y + eyeExtent.y * eyeRectMin.y,
                        leftCenter.x + eyeExtent.x * eyeRectMax.x,
                        leftCenter.y + eyeExtent.y * eyeRectMax.y);

                Graphics.DrawTexture(leftScreen, eyeOverlay.imageTexture, textureRect, 0, 0, 0, 0);
            }
            else if (eyeOverlay.eyeSide == Pvr_UnitySDKAPI.Eye.RightEye)
            {
                rightScreen = Rect.MinMaxRect(
                       rightCenter.x + eyeExtent.x * eyeRectMin.x,
                       rightCenter.y + eyeExtent.y * eyeRectMin.y,
                       rightCenter.x + eyeExtent.x * eyeRectMax.x,
                       rightCenter.y + eyeExtent.y * eyeRectMax.y);

                Graphics.DrawTexture(rightScreen, eyeOverlay.imageTexture, textureRect, 0, 0, 0, 0);
            }
        }
    }
#endif
    #endregion

    /************************************  End Of Per Frame  *************************************/
    IEnumerator EndOfFrame()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (isfirst && framenum == 3)
            {
                Pvr_UnitySDKAPI.System.UPvr_RemovePlatformLogo();
                Pvr_UnitySDKAPI.System.UPvr_StartVRModel();
                isfirst = false;
            }
            else if (isfirst && framenum < 3)
            {
                Debug.Log("+++++++++++++++++++++++++++++++" + framenum);
                framenum++;
                
                GL.Clear(false, true, Color.black);
            }

            // if find Overlay then Open Composition Layers feature
            if (Pvr_UnitySDKEyeOverlay.Instances.Count > 0)
            {
                #region Composition Layers
                Pvr_UnitySDKEyeOverlay.Instances.Sort();
                // for Overlay
                for (int i = 0; i < Overlays.Length; i++)
                {
                    if (!Overlays[i].gameObject.activeSelf) continue;
                    if (!Overlays[i].isActiveAndEnabled) continue;
                    if (Overlays[i].imageTexture == null) continue;
                    if (Overlays[i].imageTransform != null && !Overlays[i].imageTransform.gameObject.activeSelf) continue;

                    if (Overlays[i].imageType == Pvr_UnitySDKEyeOverlay.ImageType.StandardTexture)
                    {
                        // 2D Overlay Standard Texture
                        Pvr_UnitySDKAPI.Render.UPvr_SetOverlayModelViewMatrix(Overlays[i].ImageTextureId, (int)Overlays[i].eyeSide, 1, Overlays[i].MVMatrix);
                    }
                    else if(Overlays[i].imageType == Pvr_UnitySDKEyeOverlay.ImageType.EquirectangularTexture)
                    {
                        // 360 Overlay Equirectangular Texture
                        Pvr_UnitySDKAPI.Render.UPvr_SetupLayerData(0, (int)Overlays[i].eyeSide, Overlays[i].ImageTextureId, (int)Overlays[i].imageType, 0);
                    }
                }
                #endregion
            }

            Pvr_UnitySDKPluginEvent.IssueWithData(RenderEventType.TimeWarp, Pvr_UnitySDKManager.SDK.RenderviewNumber);
            Pvr_UnitySDKManager.SDK.currEyeTextureIdx = Pvr_UnitySDKManager.SDK.nextEyeTextureIdx;
            Pvr_UnitySDKManager.SDK.nextEyeTextureIdx = (Pvr_UnitySDKManager.SDK.nextEyeTextureIdx + 1) % 3;
        }
    }
}