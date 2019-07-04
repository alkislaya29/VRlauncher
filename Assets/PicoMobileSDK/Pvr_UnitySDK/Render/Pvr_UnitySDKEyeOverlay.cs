#if !UNITY_EDITOR
#if UNITY_ANDROID
#define ANDROID_DEVICE
#elif UNITY_IPHONE
#define IOS_DEVICE
#elif UNITY_STANDALONE_WIN
#define WIN_DEVICE
#endif
#endif

using Pvr_UnitySDKAPI;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Pvr_UnitySDKEyeOverlay : MonoBehaviour, IComparable<Pvr_UnitySDKEyeOverlay>
{
    public static List<Pvr_UnitySDKEyeOverlay> Instances = new List<Pvr_UnitySDKEyeOverlay>();

    public Eye eyeSide;
    [HideInInspector]
    public int layerIndex = 0;
    public ImageType imageType = ImageType.StandardTexture;
    public Texture2D imageTexture;
    // Donn't modify at Runtime
    public Transform imageTransform;

    [HideInInspector]
    public Matrix4x4 MVMatrix;


    public int ImageTextureId { get; set; }

    private Camera eyeCamera = null;

    public int CompareTo(Pvr_UnitySDKEyeOverlay other)
    {
        return this.layerIndex.CompareTo(other.layerIndex);
    }

    #region Unity Methods
    private void Awake()
    {
        Instances.Add(this);
        this.eyeCamera = this.GetComponent<Camera>();
        this.InitializeBuffer();
    }

    private void LateUpdate()
    {
        this.UpdateCoords();
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }
    #endregion




    private void InitializeBuffer()
    {
        switch (this.imageType)
        {
            case ImageType.StandardTexture:
                if (this.imageTexture)
                {
                    this.ImageTextureId = this.imageTexture.GetNativeTexturePtr().ToInt32();
                }
                break;
            case ImageType.EquirectangularTexture:
                if (this.imageTexture)
                {
                    this.ImageTextureId = this.imageTexture.GetNativeTexturePtr().ToInt32();
                }
                break;
            default:
                break;
        }
    }

    private void UpdateCoords()
    {
        if (this.imageTransform == null || !this.imageTransform.gameObject.activeSelf)
        {
            return;
        }

        if (this.eyeCamera == null)
        {
            return;
        }

        if (imageType == ImageType.StandardTexture)
        {
            // update MV matrix
            this.MVMatrix = eyeCamera.worldToCameraMatrix * imageTransform.localToWorldMatrix;
        }
    }

    #region Public Method
    public void SetTexture(Texture2D texture)
    {
        this.imageTexture = texture;
        this.InitializeBuffer();
    }

    #endregion

    public enum ImageType
    {
        StandardTexture = 0,
        //EglTexture = 1,
        EquirectangularTexture = 2
    }
}
