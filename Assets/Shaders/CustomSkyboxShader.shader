/*
Cubemap shader with adjustable vertical Scale/Offset and HSV color controls.
Creation goal was to enable user to create several casually-looking skyboxes in different colors from single image, drawn in 2d editing software.
Features:

- Doesn't requires 6-sides cubemap, or spherical projection: texture can be easily drawn in any 2d image editing software;
- "V Offset" option allows to move horizon line up and down with ease;
- "V Scale" allows to stretch texture;
- Rotation feature sets horizontal offset (as in original shader).
- HSV controls allows to change colors.


Based on default Unity's Skybox/Cubemap shader
TransformHSV code was found somewhere over the internet years ago, so I'm honestly cant remember any copyright.

No copyrights, do with this code whatever you want, without any responsibility from my side.

Demo:
https://youtu.be/jJpai7qjNOE

M. Khadzhynov, 2021
*/

Shader "GG/Skybox/Cubemap Horizon HSV"
{
    Properties
    {
        _Tint("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
        _Rotation("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex("Cubemap   (HDR)", Cube) = "grey" {}

        _VOffset("V Offset", Range(-1, 1)) = 0
        _VScale("V Scale", Range(-4, 4)) = 1

        _ShiftHue("Shift hue", Range(0, 360)) = 0
        _ShiftSaturation("Shift saturation", Range(0, 5)) = 1
        _ShiftValue("Shift value", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
         Cull Off ZWrite Off

        Pass 
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            fixed _VOffset;
            fixed _VScale;

            half _ShiftHue;
            half _ShiftSaturation;
            half _ShiftValue;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            float3 TransformHSV(float3 col)
            {
                float uAngle = _ShiftHue * 3.14 / 180;
                float wAngle = _ShiftHue * 3.14 / 180;

                float uMultipler = _ShiftValue * _ShiftSaturation * cos(uAngle);
                float wMultipler = _ShiftValue * _ShiftSaturation * sin(wAngle);

                float3 ret = col;
                ret.r = (0.299 * _ShiftValue + 0.701 * uMultipler + 0.168 * wMultipler) * col.r
                    + (0.587 * _ShiftValue - 0.587 * uMultipler + 0.330 * wMultipler) * col.g
                    + (0.114 * _ShiftValue - 0.114 * uMultipler - 0.497 * wMultipler) * col.b;

                ret.g = (0.299 * _ShiftValue - 0.299 * uMultipler - 0.328 * wMultipler) * col.r
                    + (0.587 * _ShiftValue + 0.413 * uMultipler + 0.035 * wMultipler) * col.g
                    + (0.114 * _ShiftValue - 0.114 * uMultipler + 0.292 * wMultipler) * col.b;

                ret.b = (0.299 * _ShiftValue - 0.3 * uMultipler + 1.25 * wMultipler) * col.r
                    + (0.587 * _ShiftValue - 0.588 * uMultipler - 1.05 * wMultipler) * col.g
                    + (0.114 * _ShiftValue + 0.886 * uMultipler - 0.203 * wMultipler) * col.b;

                return ret;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 uv = fixed3(
                    i.texcoord.x,
                    i.texcoord.y * _VScale + _VOffset, 
                    i.texcoord.z);

                half4 tex = texCUBE(_Tex, uv);
                half3 c = DecodeHDR(tex, _Tex_HDR);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;

                c.rgb = TransformHSV(c.rgb);

                return half4(c, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}