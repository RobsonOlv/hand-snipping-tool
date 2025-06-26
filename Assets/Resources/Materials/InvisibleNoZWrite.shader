Shader "Custom/InvisibleNoZWrite"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Color [_Color]
        }
    }
}