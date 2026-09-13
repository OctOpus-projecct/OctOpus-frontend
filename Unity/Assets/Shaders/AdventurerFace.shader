Shader "OctOpus/Adventurer Face"
{
    Properties { _Color ("Skin tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        struct Input { float3 localPosition; float4 color : COLOR; };
        fixed4 _Color;
        void vert(inout appdata_full v,out Input o)
        { UNITY_INITIALIZE_OUTPUT(Input,o);o.localPosition=v.vertex.xyz;o.color=v.color; }
        float ellipse(float2 p,float2 size)
        {return 1-smoothstep(.97,1.03,length(p/size));}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 color=IN.color.rgb*_Color.rgb;
            float2 p=IN.localPosition.xy;
            float2 q=p-float2(p.x<0?-.137:.137,.017);
            float front=smoothstep(.15,.22,IN.localPosition.z);
            float outer=ellipse(q,float2(.079,.097))*front;
            float inner=ellipse(q,float2(.073,.089));
            float3 eye=lerp(color,float3(.15,.065,.037),smoothstep(-.04,.03,q.y));
            eye=lerp(eye,float3(.99,.96,.87),inner);
            float2 irisPoint=q-float2(0,-.006);
            float iris=ellipse(irisPoint,float2(.053,.079));
            float3 irisColor=lerp(float3(.50,.28,.11),float3(.12,.045,.019),smoothstep(-.055,.055,q.y));
            float rim=smoothstep(.84,.98,length(irisPoint/float2(.053,.079)));
            irisColor=lerp(irisColor,float3(.11,.044,.021),rim);
            eye=lerp(eye,irisColor,iris);
            eye=lerp(eye,float3(.025,.014,.012),ellipse(q-float2(0,.01),float2(.027,.058))*iris);
            eye=lerp(eye,float3(1,.99,.96),ellipse(q-float2(-.021,.042),float2(.014,.018)));
            eye=lerp(eye,float3(1,.86,.57),ellipse(q-float2(.020,-.047),float2(.006,.007)));
            color=lerp(color,eye,outer);
            float bx=abs(p.x)-.137;
            float browY=.153+.016*(1-pow(bx/.073,2));
            float brow=(1-smoothstep(.005,.009,abs(p.y-browY)))*(1-smoothstep(.058,.074,abs(bx)))*front;
            color=lerp(color,float3(.24,.13,.07),brow);
            float mouth=(1-smoothstep(.002,.004,abs(p.y-(-.143-.001*pow(p.x/.022,2)))))*(1-smoothstep(.017,.023,abs(p.x)))*front;
            color=lerp(color,float3(.46,.25,.20),mouth);
            o.Albedo=color;o.Metallic=0;o.Smoothness=.17;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
