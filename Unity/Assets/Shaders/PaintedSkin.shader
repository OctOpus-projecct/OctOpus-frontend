Shader "OctOpus/Painted Skin" {
 Properties { _Color("Skin",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 struct Input { float4 tint; };
 fixed4 _Color;
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.tint=v.color;}
 void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=_Color.rgb*IN.tint.rgb;o.Smoothness=.16;o.Alpha=1;}
 ENDCG
 }
 Fallback "Diffuse"
}
