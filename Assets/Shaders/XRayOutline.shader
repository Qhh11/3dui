Shader "Custom/XRayOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0.5, 1, 0.7)
        _OutlineWidth ("Outline Width", Range(0.01, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }

        // Only render when BEHIND other objects
        ZTest Greater
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                // Push vertices outward along normal = outline
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}