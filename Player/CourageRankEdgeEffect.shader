Shader "Custom/CourageRankEdgeEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1, 1, 0.5, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2)) = 1.0
        _SparkleSpeed ("Sparkle Speed", Range(0, 10)) = 2.0
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5.0
        _EffectIntensity ("Effect Intensity", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _SparkleIntensity;
            float _SparkleSpeed;
            float _NoiseScale;
            float _EffectIntensity;
            
            // 간단한 노이즈 함수
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // 부드러운 노이즈
            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // UV를 중심으로부터의 거리 계산
                float2 center = float2(0.5, 0.5);
                float2 distFromCenter = abs(i.uv - center);
                float maxDist = max(distFromCenter.x, distFromCenter.y);
                
                // 가장자리 거리 계산 (0 = 중심, 0.5 = 가장자리)
                float edgeDistance = maxDist * 2.0; // 0 ~ 1 범위로 변환
                
                // 가장자리 영역 계산 (0 = 중심, 1 = 가장자리)
                float edgeFactor = 1.0 - smoothstep(0.0, _EdgeWidth, 1.0 - edgeDistance);
                
                // 반짝임 효과를 위한 시간 기반 노이즈
                float time = _Time.y * _SparkleSpeed;
                float2 noiseUV = i.uv * _NoiseScale + time * 0.1;
                float sparkleNoise = smoothNoise(noiseUV);
                
                // 반짝임 강도 계산 (시간에 따라 변화)
                float sparkleWave = sin(time * 2.0) * 0.5 + 0.5; // 0 ~ 1 범위
                float sparkleIntensity = sparkleNoise * sparkleWave * _SparkleIntensity;
                
                // 가장자리 색상 계산
                float3 edgeColor = _EdgeColor.rgb * (1.0 + sparkleIntensity);
                
                // 알파 계산 (가장자리일수록 강하게, 중심으로 갈수록 투명하게)
                // _EdgeColor.a가 0이어도 최소한의 알파는 보장
                float minAlpha = 0.01; // 최소 알파 값 (거의 투명하지만 보이도록)
                float colorAlpha = max(_EdgeColor.a, minAlpha);
                float alpha = edgeFactor * _EffectIntensity * colorAlpha;
                
                // 최종 색상
                fixed4 col = fixed4(edgeColor, alpha);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}

