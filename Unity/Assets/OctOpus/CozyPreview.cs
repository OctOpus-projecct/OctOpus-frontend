using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using OctOpus.Shared;
using UnityEngine;

// Opt-in render capture used to verify the real Unity UI, never enabled in normal play.
public sealed class CozyPreview : MonoBehaviour
{
    private IEnumerator Start()
    {
        string directory=Environment.GetEnvironmentVariable("OCTOPUS_UI_CAPTURE");
        if(string.IsNullOrEmpty(directory)) yield break;
        Directory.CreateDirectory(directory);
        yield return new WaitForSecondsRealtime(3);
        var client=GetComponent<ClientBootstrap>();
        yield return Capture(Path.Combine(directory,"login.png"));
        yield return new WaitForSecondsRealtime(1);
        client.Connect();
        NetworkPlayer owner=null;
        float deadline=Time.realtimeSinceStartup+30;
        while((owner=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsOwner))==null && Time.realtimeSinceStartup<deadline) yield return null;
        if(owner==null) { Debug.LogError("[OctOpus] UiPreview=Failed: no owner"); yield break; }
        yield return new WaitForSecondsRealtime(2);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var block=typeof(ClientBootstrap).GetMethod("UiBlocksWorld",flags);
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Func<float,float,bool> blocked=(x,y)=>(bool)block.Invoke(client,new object[]{new Vector2(x*scale,Screen.height-y*scale)});
        if(!blocked(Screen.width/scale-150,50) || blocked(400,200))
        { Debug.LogError("[OctOpus] UiPreview=Failed: menu or free ground input"); yield break; }
        yield return Capture(Path.Combine(directory,"hud.png"));
        yield return new WaitForSecondsRealtime(1);
        typeof(ClientBootstrap).GetField("bagOpen",flags).SetValue(client,true);
        if(!blocked(Screen.width/scale-150,220)) { Debug.LogError("[OctOpus] UiPreview=Failed: bag input"); yield break; }
        var tree=FindObjectsByType<NetworkTree>(FindObjectsSortMode.None).First();
        typeof(ClientBootstrap).GetField("selectedTree",flags).SetValue(client,tree);
        owner.RequestWork(tree.NetworkObject);
        yield return new WaitForSecondsRealtime(6);
        yield return Capture(Path.Combine(directory,"bag-tree.png"));
        yield return new WaitForSecondsRealtime(1);
        typeof(ClientBootstrap).GetField("bagOpen",flags).SetValue(client,false);
        typeof(ClientBootstrap).GetField("settingsOpen",flags).SetValue(client,true);
        yield return new WaitForSecondsRealtime(1);
        yield return Capture(Path.Combine(directory,"settings.png"));
        yield return new WaitForSecondsRealtime(1);
        Debug.Log("[OctOpus] UiPreview=Passed");
    }
    private IEnumerator Capture(string path)
    {
        yield return new WaitForEndOfFrame();
        Texture2D texture=ScreenCapture.CaptureScreenshotAsTexture();
        if(texture==null) throw new InvalidOperationException("Capture texture unavailable");
        bool visible=false;
        for(int y=0;y<texture.height;y+=32) for(int x=0;x<texture.width;x+=32)
            if(texture.GetPixel(x,y).grayscale>.05f) visible=true;
        if(!visible) { Destroy(texture); throw new InvalidOperationException("Capture is black; use a visible preview window"); }
        File.WriteAllBytes(path,ImageConversion.EncodeToPNG(texture));
        Destroy(texture);
    }
}
