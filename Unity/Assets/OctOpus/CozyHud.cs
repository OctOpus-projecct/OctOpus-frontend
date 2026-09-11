using System;
using System.Linq;
using OctOpus.Shared;
using UnityEngine;

public sealed partial class ClientBootstrap
{
    private bool bagOpen, settingsOpen, debugOpen, connectionOptions;
    private GUIStyle cardStyle, titleStyle, textStyle, smallStyle, buttonStyle, fieldStyle;
    private Texture2D creamTexture, greenTexture, trackTexture;
    private Font uiFont;
    private Vector2 loginScroll;
    private float UiScale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
    private float UiWidth => Screen.width / Mathf.Max(.1f, UiScale);
    private float UiHeight => Screen.height / Mathf.Max(.1f, UiScale);
    private Rect MenuRect => new Rect(UiWidth - 276, 24, 252, 54);
    private Rect HudRect => new Rect((UiWidth - 460) / 2, UiHeight - 112, 460, 88);
    private Rect BagRect => new Rect(UiWidth - 384, 94, 360, 336);
    private Rect SettingsRect => new Rect((UiWidth - 420) / 2, (UiHeight - 450) / 2, 420, 450);
    private Rect TreeCardRect()
    {
        if (selectedTree == null || Camera.main == null) return Rect.zero;
        Vector3 point = Camera.main.WorldToScreenPoint(selectedTree.transform.position + Vector3.up * 3.5f);
        if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return Rect.zero;
        return new Rect(Mathf.Clamp(point.x / UiScale - 120, 12, UiWidth - 252),
            Mathf.Clamp((Screen.height - point.y) / UiScale - 84, 100, UiHeight - 220), 240, 84);
    }
    private bool UiBlocksWorld(Vector2 point)
    {
        if (!players.Any(p => p != null && p.IsOwner)) return true;
        Vector2 ui = new Vector2(point.x, Screen.height - point.y) / UiScale;
        return MenuRect.Contains(ui) || HudRect.Contains(ui) || new Rect(24, 24, 230, 72).Contains(ui) ||
            (bagOpen && BagRect.Contains(ui)) || (settingsOpen && SettingsRect.Contains(ui)) ||
            (debugOpen && MovementInput.PanelRect.Contains(ui)) || TreeCardRect().Contains(ui);
    }
    private void EnsureSkin()
    {
        if (cardStyle != null) return;
        creamTexture = Rounded(new Color(.99f, .96f, .87f));
        greenTexture = Rounded(new Color(.38f, .56f, .38f));
        trackTexture = Rounded(new Color(.85f, .87f, .76f));
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
        cardStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(12,12,12,12), padding = new RectOffset(24,24,20,20) };
        cardStyle.normal.background = creamTexture;
        textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 17, wordWrap = true };
        textStyle.normal.textColor = new Color(.25f,.32f,.25f);
        smallStyle = new GUIStyle(textStyle) { fontSize = 13 };
        smallStyle.normal.textColor = new Color(.46f,.50f,.40f);
        titleStyle = new GUIStyle(textStyle) { fontSize = 28, fontStyle = FontStyle.Bold };
        buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 16, fixedHeight = 40, border = new RectOffset(12,12,12,12) };
        foreach (GUIStyleState s in new[] { buttonStyle.normal, buttonStyle.hover, buttonStyle.active, buttonStyle.focused })
        { s.background = greenTexture; s.textColor = Color.white; }
        fieldStyle = new GUIStyle(GUI.skin.textField) { font = uiFont, fontSize = 17, fixedHeight = 36, padding = new RectOffset(12,12,6,6), border = new RectOffset(12,12,12,12) };
        foreach (GUIStyleState s in new[] { fieldStyle.normal, fieldStyle.hover, fieldStyle.focused })
        { s.background = trackTexture; s.textColor = textStyle.normal.textColor; }
    }
    private static Texture2D Rounded(Color color)
    {
        var texture = new Texture2D(32,32,TextureFormat.RGBA32,false) { hideFlags = HideFlags.HideAndDontSave };
        for (int y=0;y<32;y++) for (int x=0;x<32;x++)
        {
            float dx = Mathf.Max(9-x, x-22, 0), dy = Mathf.Max(9-y,y-22,0);
            Color pixel = color; pixel.a *= Mathf.Clamp01(9.5f-Mathf.Sqrt(dx*dx+dy*dy));
            texture.SetPixel(x,y,pixel);
        }
        texture.Apply(); return texture;
    }
    private void Panel(Rect rect) { GUI.Box(rect, GUIContent.none, cardStyle); }
    private void Label(Rect rect, string value, bool small=false) { GUI.Label(rect,value,small ? smallStyle : textStyle); }
    private void Meter(Rect rect, float value, float maximum)
    {
        GUI.DrawTexture(rect,trackTexture);
        float fill = maximum > 0 ? Mathf.Clamp01(value / maximum) : 0;
        if (fill > 0) GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width*fill,rect.height),greenTexture);
    }
    private void OnGUI()
    {
        if (manager == null) return;
        EnsureSkin();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * UiScale);
        if (clearGuiFocus) { GUI.FocusControl(null); clearGuiFocus = false; }
        if (rebindingStrike && Event.current.type == EventType.KeyDown)
        {
            KeyCode key = Event.current.keyCode;
            if (key == KeyCode.Escape || StrikeInput.IsBindable(key))
            {
                if (key != KeyCode.Escape) strikeInput.SetKey(key,Time.frameCount);
                else strikeInput.UpdateFocus(Time.frameCount);
                rebindingStrike = false; GUI.FocusControl(null); Event.current.Use();
            }
        }
        var owner = players.FirstOrDefault(p => p != null && p.IsOwner);
        if (owner == null) DrawLoginCard();
        else
        {
            Panel(new Rect(24,24,230,72));
            Label(new Rect(44,36,195,26),"OctOpus  /  작은 숲");
            Label(new Rect(44,66,195,22),"함께 머무는 중 · " + roster.Length + "명",true);
            Panel(MenuRect);
            if (GUI.Button(new Rect(MenuRect.x+8,31,110,40),bagOpen ? "가방 닫기" : "가방 열기",buttonStyle))
            { bagOpen = !bagOpen; GUI.FocusControl(null); }
            if (GUI.Button(new Rect(MenuRect.x+128,31,116,40),"설정",buttonStyle))
            { settingsOpen = !settingsOpen; rebindingStrike=false; GUI.FocusControl(null); }
            DrawBottomHud(owner);
            DrawTreeCard(owner);
            if (bagOpen) DrawBag(owner);
            if (settingsOpen) DrawSettings();
            if (debugOpen) DrawDebugPanel();
        }
        uiHasKeyboardFocus = GUIUtility.keyboardControl != 0;
        GUI.matrix = previous;
    }
    private void DrawLoginCard()
    {
        Color old=GUI.color; GUI.color=new Color(.19f,.28f,.22f,.65f);
        GUI.DrawTexture(new Rect(0,0,UiWidth,UiHeight),Texture2D.whiteTexture); GUI.color=old;
        float x=(UiWidth-460)/2;
        Rect rect=new Rect(x,32,460,UiHeight-64); Panel(rect);
        GUI.Label(new Rect(x+36,62,388,54),"OctOpus",new GUIStyle(titleStyle){fontSize=42});
        Label(new Rect(x+38,120,384,28),"작은 숲에서 시작하는, 우리의 하루",true);
        GUILayout.BeginArea(new Rect(x+36,166,388,UiHeight-216));
        loginScroll=GUILayout.BeginScrollView(loginScroll);
        GUILayout.Label("모험가 이름",textStyle);
        GUI.enabled=state==FishNet.Transporting.LocalConnectionState.Stopped;
        loginName=GUILayout.TextField(loginName,32,fieldStyle);
        GUILayout.Space(12); GUILayout.Label("비밀번호",textStyle);
        loginPassword=GUILayout.PasswordField(loginPassword,'*',128,fieldStyle);
        GUILayout.Space(12); GUILayout.Label("서버 주소",textStyle);
        address=GUILayout.TextField(address,253,fieldStyle);
        GUILayout.Space(12);
        if (GUILayout.Button(connectionOptions ? "서버 인증 설정 접기" : "서버 인증 설정",buttonStyle)) connectionOptions=!connectionOptions;
        if(connectionOptions)
        { GUILayout.Label("서버에서 받은 인증서 지문",smallStyle); certificatePin=GUILayout.TextField(certificatePin,64,fieldStyle); }
        GUILayout.Space(12);
        if(GUILayout.Button("숲으로 들어가기",buttonStyle)) Connect();
        GUI.enabled=true;
        if(state!=FishNet.Transporting.LocalConnectionState.Stopped && GUILayout.Button("연결 취소",buttonStyle)) Disconnect();
        GUILayout.Space(10);
        GUILayout.Label(LoginFailed ? "접속하지 못했어요. 계정 정보와 서버 설정을 확인해 주세요." :
            state==FishNet.Transporting.LocalConnectionState.Stopped ? "계정은 서버 관리자가 만들어 드려요." : "숲에 연결하고 있어요…",smallStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    private void DrawBottomHud(NetworkPlayer owner)
    {
        Rect r=HudRect; Panel(r);
        // Simple tool silhouette, drawn in the same palette as the interface.
        Color old=GUI.color; GUI.color=new Color(.52f,.34f,.20f);
        GUI.DrawTexture(new Rect(r.x+37,r.y+27,9,40),Texture2D.whiteTexture);
        GUI.color=new Color(.55f,.67f,.64f); GUI.DrawTexture(new Rect(r.x+41,r.y+22,25,18),trackTexture); GUI.color=old;
        Label(new Rect(r.x+84,r.y+14,345,24),"생활 도끼  ·  " + strikeInput.Key);
        Meter(new Rect(r.x+84,r.y+43,240,12),owner.Stamina,owner.MaximumStamina);
        Label(new Rect(r.x+334,r.y+34,105,28),Mathf.CeilToInt(owner.Stamina)+" / "+owner.MaximumStamina,true);
        string hint=owner.Activity==PlayerActivity.Working ? "키를 눌러 벌목 · 지면 클릭으로 취소" : "나무를 클릭해서 벌목을 시작하세요";
        Label(new Rect(r.x+84,r.y+61,350,22),hint,true);
    }
    private void DrawTreeCard(NetworkPlayer owner)
    {
        Rect r=TreeCardRect(); if(r.width==0) return; Panel(r);
        string name=selectedTree.IsDepleted ? "다시 자라는 중" : selectedTree.WorkerId<0 ? "벌목할 나무" : selectedTree.WorkerId==owner.OwnerId ? "나무를 베고 있어요" : "다른 모험가가 작업 중";
        Label(new Rect(r.x+16,r.y+9,210,26),name);
        Meter(new Rect(r.x+16,r.y+39,208,10),selectedTree.Health,selectedTree.MaximumHealth);
        Label(new Rect(r.x+16,r.y+55,210,24), selectedTree.IsDepleted ? $"재생성까지 {selectedTree.RespawnRemaining:0.0}초" :
            $"{selectedTree.Health:0} / {selectedTree.MaximumHealth:0}"+(selectedTree.WorkerId>=0 ? $"   ·   {selectedTree.SecondsRemaining:0.0}초" : ""),true);
    }
    private void DrawBag(NetworkPlayer owner)
    {
        Rect r=BagRect; Panel(r); GUI.Label(new Rect(r.x+24,r.y+20,300,40),"나의 가방",titleStyle);
        var inv=owner.Inventory;
        if(inv.CapacityUnits<=0) { Label(new Rect(r.x+24,r.y+80,300,30),"소지품을 불러오고 있어요…"); return; }
        Color old=GUI.color; GUI.color=new Color(.64f,.43f,.26f);
        GUI.DrawTexture(new Rect(r.x+26,r.y+92,56,40),trackTexture); GUI.color=old;
        Label(new Rect(r.x+100,r.y+82,190,28),"목재");
        Label(new Rect(r.x+100,r.y+113,190,28),inv.WoodCount+"개");
        Label(new Rect(r.x+24,r.y+179,312,28),$"무게   {inv.WeightUnits/(float)InventoryRules.WeightUnitsPerUnit:0.###} / {inv.CapacityUnits/(float)InventoryRules.WeightUnitsPerUnit:0.###}");
        Meter(new Rect(r.x+24,r.y+217,312,12),inv.WeightUnits,inv.CapacityUnits);
        Label(new Rect(r.x+24,r.y+245,312,64), inv.IsOverweight ? "가방이 무거워 이동이 느려져요.\n새 채집을 시작할 수 없어요." : !inv.CanGather ? "가방 무게가 95% 이상이에요.\n새 채집을 시작할 수 없어요." : "여유가 있어요. 숲에서 자원을 모아보세요.",true);
    }
    private void DrawSettings()
    {
        Panel(SettingsRect); GUILayout.BeginArea(new Rect(SettingsRect.x+28,SettingsRect.y+20,364,408));
        GUILayout.Label("설정",titleStyle); GUILayout.Space(16);
        GUILayout.Label("이동 / 나무 선택 버튼",textStyle);
        int button=GUILayout.Toolbar(movementInput.Button,new[]{"왼쪽","오른쪽","가운데"},buttonStyle);
        if(button!=movementInput.Button) movementInput.SetButton(button);
        GUILayout.Space(16);
        if(GUILayout.Button(rebindingStrike ? "바꿀 키를 누르세요 (Esc 취소)" : "벌목 키  ·  "+strikeInput.Key,buttonStyle))
        { rebindingStrike=!rebindingStrike; strikeInput.UpdateFocus(Time.frameCount); }
        GUILayout.Space(16);
        if(GUILayout.Button(debugOpen ? "개발 정보 숨기기" : "개발 정보 보기",buttonStyle)) debugOpen=!debugOpen;
        GUILayout.Space(16);
        if(GUILayout.Button("접속 종료",buttonStyle)) { settingsOpen=false; bagOpen=false; debugOpen=false; Disconnect(); }
        GUILayout.Space(16);
        if(GUILayout.Button("돌아가기",buttonStyle)) { settingsOpen=false; rebindingStrike=false; GUI.FocusControl(null); }
        GUILayout.EndArea();
    }
    private void DisposeSkin()
    {
        if(creamTexture!=null) Destroy(creamTexture);
        if(greenTexture!=null) Destroy(greenTexture);
        if(trackTexture!=null) Destroy(trackTexture);
        if(uiFont!=null) Destroy(uiFont);
    }
}
