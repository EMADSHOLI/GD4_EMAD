using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class FarmMinimap : MonoBehaviour
{
    public Transform player;
    public RawImage mapImage;
    public RectTransform playerArrow;
    public GameObject mapPanel;
    [Min(10)] public float viewRadius = 45f;
    public Camera MapCamera { get; private set; }
    RenderTexture texture;
    void Start()
    {
        texture = new RenderTexture(512,512,16) { name="FarmMinimapTexture" };
        texture.Create();
        var cameraObject = new GameObject("FarmMinimapCamera",typeof(Camera));
        cameraObject.transform.SetParent(transform,false);
        MapCamera = cameraObject.GetComponent<Camera>();
        MapCamera.orthographic=true; MapCamera.nearClipPlane=.3f; MapCamera.farClipPlane=450f;
        MapCamera.clearFlags=CameraClearFlags.SolidColor; MapCamera.backgroundColor=new Color(.16f,.22f,.17f);
        MapCamera.cullingMask=~(1<<5); MapCamera.targetTexture=texture; MapCamera.allowHDR=false; MapCamera.allowMSAA=false;
        var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderShadows=false; data.renderPostProcessing=false;
        if(mapImage)mapImage.texture=texture;
        Follow();
    }
    void LateUpdate()
    {
        bool visible=!FarmShop.IsOpen;
        if(mapPanel && mapPanel.activeSelf!=visible)mapPanel.SetActive(visible);
        if(MapCamera)MapCamera.enabled=visible && player;
        Follow();
    }
    public void Follow()
    {
        if(!player || !MapCamera)return;
        MapCamera.orthographicSize=Mathf.Max(10,viewRadius);
        MapCamera.transform.SetPositionAndRotation(player.position+Vector3.up*200f,Quaternion.Euler(90,0,0));
        if(playerArrow)playerArrow.localRotation=Quaternion.Euler(0,0,-player.eulerAngles.y);
    }
    void OnDestroy()
    {
        if(MapCamera){MapCamera.targetTexture=null;Destroy(MapCamera.gameObject);}
        if(mapImage)mapImage.texture=null;
        if(texture){texture.Release();Destroy(texture);}
    }
}
