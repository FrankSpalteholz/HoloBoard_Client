Shader "Custom/UVWarpingWithViewerPersp"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutoff", Range(0.0, 1.0)) = 0.5

        // Tiling - Standardwert für Y auf 2.164
        _Tiling("Tiling", Vector) = (1, 2.164, 0, 0)

        // Offset - Standardwert für Y auf 0.418
        _Offset("Offset", Vector) = (0.0, 0.418, 0.0, 0.0)

        // Kamera Position und Rotation
        _ViewerPosition("Viewer Position", Vector) = (0, 0, 0, 0)
        _ViewerRotation("Viewer Rotation", Vector) = (0, 0, 0, 0)

        // Bildschirm-Position und Größe mit Default-Werten (52.4 cm x 24.213 cm)
        _ScreenPosition("Screen Position", Vector) = (0, 0, 0, 0)
        _ScreenSize("Screen Size", Vector) = (52.4, 24.213, 1, 1) // Default: 52.4 cm Breite, 24.213 cm Höhe

        _ScreenScaleFactor("Screen Scale Factor", Float) = 52.4 // Standardmäßig Breite als Skalierungsfaktor

        // Cull Mode
        _FaceToRender("Face To Render", Float) = 2.0

        // Toggle für Translation oder Translation + Rotation
        _UseRotation("Use Rotation (toggle)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull [_FaceToRender]

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
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _ViewerPosition;
            float3 _ViewerRotation; 
            float3 _ScreenPosition; 
            float3 _ScreenSize; 
            float _ScreenScaleFactor;
            float3 _ScreenNormal;
            float _DistortionFactor;
            float4 _Tiling;
            float4 _Offset;
            float _UseRotation;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Blickrichtung des Betrachters berechnen
                float3 viewDir = normalize(_ViewerPosition - v.vertex.xyz);

                if (_UseRotation > 0.5)
                {
                    float3 forward = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, _ViewerRotation));
                    viewDir = normalize(_ViewerPosition - v.vertex.xyz);
                }

                // Winkel zwischen Blickrichtung und Bildschirm-Normale berechnen
                float angle = acos(dot(viewDir, normalize(_ScreenNormal)));
                float distortion = angle * _DistortionFactor; 

                // **Skalierungsfaktor in die Verzerrung einbeziehen**
                distortion /= _ScreenScaleFactor;

                // UV-Verzerrung
                o.uv = v.uv + distortion * 0.1;

                // Tiling anwenden
                o.uv *= _Tiling.xy; 
                // Offset anwenden
                o.uv += _Offset.xy; 

                return o;
            }

            sampler2D _MainTex;
            float4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
