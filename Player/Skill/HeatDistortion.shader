Shader "Custom/HeatDistortion"
{
    Properties
    {
        _MainTex ("Distortion Texture (Noise)", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _Speed ("Distortion Speed", Range(0, 5)) = 1.0
        _Color ("Tint Color", Color) = (1, 1, 1, 0.5)
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        // 배경을 왜곡하기 위해 GrabPass 사용
        GrabPass { "_BackgroundTexture" }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            sampler2D _BackgroundTexture;
            float4 _MainTex_ST;
            float _DistortionStrength;
            float _Speed;
            float4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 시간에 따라 움직이는 노이즈 샘플링
                float2 distortUV = i.uv + _Time.y * _Speed * 0.1;
                
                // 노이즈 텍스처에서 왜곡 값 가져오기
                float2 distortion = tex2D(_MainTex, distortUV).rg;
                distortion = (distortion - 0.5) * 2.0; // -1 ~ 1 범위로 변환
                
                // 두 번째 노이즈 레이어 (다른 속도)
                float2 distortUV2 = i.uv * 1.5 - _Time.y * _Speed * 0.15;
                float2 distortion2 = tex2D(_MainTex, distortUV2).rg;
                distortion2 = (distortion2 - 0.5) * 2.0;
                
                // 두 노이즈 레이어 합성
                distortion = (distortion + distortion2) * 0.5;
                
                // 배경 텍스처 샘플링 (왜곡 적용)
                float2 grabUV = i.grabPos.xy / i.grabPos.w;
                grabUV += distortion * _DistortionStrength;
                
                // 배경색 가져오기
                fixed4 backgroundColor = tex2D(_BackgroundTexture, grabUV);
                
                // 틴트 컬러 적용
                backgroundColor.rgb = lerp(backgroundColor.rgb, _Color.rgb, _Color.a * 0.3);
                
                // 가장자리로 갈수록 투명하게 (부드러운 페이드)
                float2 centerDist = abs(i.uv - 0.5) * 2.0;
                float edgeFade = 1.0 - max(centerDist.x, centerDist.y);
                edgeFade = smoothstep(0.0, 0.5, edgeFade);
                
                backgroundColor.a = edgeFade * _Color.a;
                
                return backgroundColor;
            }
            ENDCG
        }
    }
    
    // URP 호환성을 위한 폴백
    Fallback "Sprites/Default"
}

