// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Custom/KlyteTextBoards" {
    Properties
    {
        _Color("Main Color", Vector) = (1,1,1,1)
        _ColorV0("Color Variation 0", Vector) = (1,1,1,1)
        _ColorV1("Color Variation 1", Vector) = (1,1,1,1)
        _ColorV2("Color Variation 2", Vector) = (1,1,1,1)
        _ColorV3("Color Variation 3", Vector) = (1,1,1,1)
        _SpecColor("Specular Color", Vector) = (0.5,0.5,0.5,0)
        _MainTex("Diffuse (RGB)", 2D) = "gray" {}
        _XYSMap("NormalX/NormalY/Specular (RGB)", 2D) = "bump" {}
        _ACIMap("Alpha/ColorMask/Illumination (RGB)", 2D) = "black" {}
        _RollLocation0("Roll Location 0", Vector) = (0,0,0,1)
        _RollLocation1("Roll Location 1", Vector) = (0,0,0,1)
        _RollLocation2("Roll Location 2", Vector) = (0,0,0,1)
        _RollLocation3("Roll Location 3", Vector) = (0,0,0,1)
        _RollParams0("Roll Params 0", Vector) = (0,1,0,10)
        _RollParams1("Roll Params 1", Vector) = (0,1,0,10)
        _RollParams2("Roll Params 2", Vector) = (0,1,0,10)
        _RollParams3("Roll Params 3", Vector) = (0,1,0,10)
        _AtlasRect("Atlas Rect", Vector) = (0,0,1,1)
        _ObjectIndex("?, ?, Illum, ?", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest+10"
            "FORCENOSHADOWCASTING" = "true"

        }

        Cull Back
        Lighting Off
        ZTest LEqual
        ZWrite On

        CGPROGRAM
        #pragma surface surf Deferred decal:blend
        #include "UnityLightingCommon.cginc"

        uniform 	fixed4 _SimulationTime;
        uniform 	fixed4 _WeatherParams; // Temp, Rain, Fog, Wetness
        sampler2D _MainTex;
        sampler2D _XYSMap;
        sampler2D _ACIMap;
        fixed4 _Color;
        fixed4 _ObjectIndex;
        fixed _BumpIntensity;
        fixed _Emission;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            fixed4 color;
        };
        // inline fixed4 LightingShadowOnly(SurfaceOutput s, fixed3 lightDir, fixed atten)
        // {
            //     fixed NdotL = dot((.50f, .0f, 0.2f), lightDir)*0.8f + 0.2f;
            //     fixed4 c;
            //     c.rgb = s.Albedo * 0.5f * NdotL * atten * length(_LightColor0);
            //     c.a = s.Alpha;
            //     return c;
        // }

        half4 LightingDeferred_PrePass(inout SurfaceOutput s, half4 light)
        {
            half4 c;
            c.rgb = s.Albedo * clamp(light.rrr, 0, 1) * clamp(light.rrr, 0, 1);
            c.a = round(s.Alpha);
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            #if defined(PIXELSNAP_ON)
                v.vertex = UnityPixelSnap (v.vertex);
            #endif

            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_ACIMap, IN.uv_MainTex);
            fixed4 s = tex2D(_XYSMap, IN.uv_MainTex);
            fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = t * _Color;
            o.Alpha = 1.0-c.r;
            o.Emission = o.Albedo * _ObjectIndex.z * 30;
            o.Specular = 0;
            o.Gloss = 50;
        }
        ENDCG

        Cull Front
        Lighting Off
        ZWrite On
        ColorMask 0
        ZTest LEqual

        CGPROGRAM
        #pragma surface surf Deferred decal:blend
        #include "UnityLightingCommon.cginc"

        sampler2D _MainTex;
        sampler2D _XYSMap;
        sampler2D _ACIMap;
        fixed4 _Color;
        fixed _BumpIntensity;
        fixed _Emission;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            fixed4 color;
        };
        // inline fixed4 LightingShadowOnly(SurfaceOutput s, fixed3 lightDir, fixed atten)
        // {
            //     fixed NdotL = dot((.50f, .0f, 0.2f), lightDir)*0.8f + 0.2f;
            //     fixed4 c;
            //     c.rgb = s.Albedo * 0.5f * NdotL * atten * length(_LightColor0);
            //     c.a = s.Alpha;
            //     return c;
        // }

        half4 LightingDeferred_PrePass(inout SurfaceOutput s, half4 light)
        {
            half4 c;
            c.rgb = s.Albedo * clamp(light.rrr, 0, 1) * clamp(light.rrr, 0, 1);
            c.a = round(s.Alpha);
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            #if defined(PIXELSNAP_ON)
                v.vertex = UnityPixelSnap (v.vertex);
            #endif

            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_ACIMap, IN.uv_MainTex);
            fixed4 s = tex2D(_XYSMap, IN.uv_MainTex);
            o.Albedo =  half4(0.001,0.001,0.001,1);
            o.Alpha = 1.0-c.r;
        }
        ENDCG
    }



}