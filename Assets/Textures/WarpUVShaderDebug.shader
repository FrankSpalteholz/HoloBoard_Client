Shader "Custom/RevisedPerspectiveWarping"
{
    Properties
    {
        _MainTex ("Render Texture", 2D) = "white" {}
        
        // Physischer Screen
        _PhysicalScreenWidth ("Physischer Screen Breite (m)", Float) = 0.524
        _PhysicalScreenHeight ("Physischer Screen Höhe (m)", Float) = 0.24213
        
        // Betrachter- und Kamera-Positionen
        _ViewerPosition ("Betrachterposition", Vector) = (0, 0, 0, 0)
        _TrackingCamPosition ("Tracking-Kamera Position", Vector) = (0, 0, 0, 0)
        _ScreenPosition ("Bildschirmposition", Vector) = (0, 0, 0, 0)
        
        // Warp-Parameter
        _WarpStrength ("Warp-Stärke", Range(0, 1)) = 0.2
        
        // Debug-Grid
        [Toggle] _ShowGrid ("Debug-Grid anzeigen", Float) = 1
        _GridSize ("Grid-Größe", Range(2, 50)) = 10
        _GridColor ("Grid-Farbe", Color) = (1, 1, 1, 0.5)
        _GridThickness ("Grid-Dicke", Range(0.001, 0.02)) = 0.006
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 distortionDebug : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PhysicalScreenWidth;
            float _PhysicalScreenHeight;
            float3 _ViewerPosition;
            float3 _TrackingCamPosition;
            float3 _ScreenPosition;
            float _WarpStrength;
            float _ShowGrid;
            float _GridSize;
            float4 _GridColor;
            float _GridThickness;

            // Hilfsfunktion zur Berechnung der perspektivischen Verzerrung
            float2 CalculatePerspectiveWarp(float2 uv, float3 viewerPos, float3 screenPos, float2 screenSize)
            {
                // Zentriere die UVs (von 0-1 auf -0.5 bis 0.5)
                float2 centeredUV = uv - 0.5;
                
                // Berechne die relative Position des Betrachters zum Bildschirm
                float3 viewerToScreen = screenPos - viewerPos;
                float viewerDistance = length(viewerToScreen);
                
                if (viewerDistance < 0.001)
                    return uv; // Verhindere Division durch Null
                
                // Normalisierte Position des Betrachters relativ zum Bildschirm
                float2 normalizedViewerPos = float2(
                    (viewerPos.x - screenPos.x) / screenSize.x,
                    (viewerPos.y - screenPos.y) / screenSize.y
                );
                
                // Berechne die Verzerrung basierend auf der relativen Position
                // Je weiter der Betrachter vom Zentrum entfernt ist, desto stärker die Verzerrung
                float2 warpFactor = normalizedViewerPos * _WarpStrength;
                
                // Anwenden der Verzerrung - stärker an den Rändern
                float2 distortion = centeredUV * warpFactor;
                
                // Berücksichtige auch den Z-Abstand für die Stärke der Verzerrung
                // Näher am Bildschirm = stärkere Verzerrung
                float zFactor = 1.0 / max(viewerToScreen.z, 0.1);
                distortion *= zFactor;
                
                // Wende die Verzerrung an
                float2 warpedUV = centeredUV - distortion;
                
                // Zurück zu 0-1 UV-Bereich
                return warpedUV + 0.5;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Beginne mit den ursprünglichen UVs
                float2 uv = v.uv;
                
                // Berechne die Screengrößen in Metern
                float2 screenSize = float2(_PhysicalScreenWidth, _PhysicalScreenHeight);
                
                // Berechne die verzerrten UVs basierend auf der Betrachterposition
                float2 warpedUV = uv;
                
                // Nur anwenden, wenn eine Betrachterposition gesetzt ist
                if (length(_ViewerPosition) > 0.001)
                {
                    // Wir haben zwei Fälle: mit oder ohne Tracking-Kamera
                    if (length(_TrackingCamPosition) > 0.001)
                    {
                        // Fall 1: Wir haben eine Tracking-Kamera
                        // Berechne die relative Position des Betrachters zur Tracking-Kamera
                        float3 viewerRelToCam = _ViewerPosition - _TrackingCamPosition;
                        
                        // Berechne die verzerrten UVs basierend auf der relativen Position
                        warpedUV = CalculatePerspectiveWarp(uv, _TrackingCamPosition + viewerRelToCam, _ScreenPosition, screenSize);
                    }
                    else
                    {
                        // Fall 2: Direkte Betrachterposition ohne Tracking-Kamera
                        warpedUV = CalculatePerspectiveWarp(uv, _ViewerPosition, _ScreenPosition, screenSize);
                    }
                }
                
                // Speichere die Debug-Informationen für den Fragment-Shader
                o.distortionDebug = warpedUV - uv;
                
                // Setze die verzerrten UVs
                o.uv = warpedUV;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture with warped UVs
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Debug Grid wenn aktiviert
                if (_ShowGrid > 0.5) {
                    // Berechne die originalen UV-Koordinaten
                    float2 gridUV = i.uv * _GridSize;
                    
                    // Bestimme, ob wir auf einer Gitterlinie sind
                    float2 grid = abs(frac(gridUV) - 0.5);
                    float gridLine = 0.0;
                    
                    // Überprüfe, ob wir auf einer horizontalen oder vertikalen Gitterlinie sind
                    if (grid.x > (0.5 - _GridThickness) || grid.y > (0.5 - _GridThickness)) {
                        gridLine = 1.0;
                    }
                    
                    // Dickere Linien für Hauptunterteilungen (alle 10 Gitterzellen)
                    float2 mainGrid = abs(frac(gridUV / 10.0) - 0.5);
                    float mainGridLine = 0.0;
                    
                    // Zeichne Hauptgitterlinien nur, wenn wir nicht an den Rändern des UV-Raums sind
                    if ((mainGrid.x > (0.5 - 0.008) || mainGrid.y > (0.5 - 0.008)) && 
                        i.uv.x > 0.01 && i.uv.x < 0.99 && 
                        i.uv.y > 0.01 && i.uv.y < 0.99) {
                        mainGridLine = 1.0;
                    }
                    
                    // Ursprungslinien (Mitte der Textur)
                    float2 distFromCenter = abs(i.uv - 0.5);
                    float centerLine = 0.0;
                    float3 centerColor = float3(1, 1, 0); // Gelb für Mittellinien
                    
                    if (distFromCenter.x < _GridThickness || distFromCenter.y < _GridThickness) {
                        centerLine = 1.0;
                    }
                    
                    // Zeichne die Linien
                    if (centerLine > 0.0) {
                        // Zentrumslinien sind gelb
                        col.rgb = lerp(col.rgb, centerColor, _GridColor.a);
                    }
                    else if (mainGridLine > 0.0) {
                        // Hauptgitterlinien sind etwas heller
                        col.rgb = lerp(col.rgb, _GridColor.rgb * 1.2, _GridColor.a);
                    }
                    else if (gridLine > 0.0) {
                        // Reguläres Gitter in der Gitterfarbe
                        col.rgb = lerp(col.rgb, _GridColor.rgb, _GridColor.a);
                    }
                    
                    // Optional: Visualisiere die Stärke der Verzerrung
                    // float distortionMagnitude = length(i.distortionDebug) * 10.0;
                    // col.r = lerp(col.r, 1.0, saturate(distortionMagnitude));
                }
                
                return col;
            }
            ENDCG
        }
    }
}