// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'


Shader "Custom/KlyteTextBoards" {
    Properties
    {
        _Color("Main Color", Vector) = (1,1,1,1)
        _BackfaceColor("Backface Color", Vector) = (0.01,0.01,0.01,1)
        _MainTex("Diffuse (RGBA)", 2D) = "transparent" {}
        _ObjectIndex("Spec, Gloss, Illum, Em.Str", Vector) = (0,0,0,0)
        _Cutout ("Alpha cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest+10"
            "RenderType" = "Vehicle" 
        }

        Cull Back
        Lighting On
        ZTest LEqual
        ZWrite On

        CGPROGRAM
        #pragma surface surf Deferred alphatest:_Cutout
        #include "UnityLightingCommon.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        float4 _MainTex_TexelSize;
        fixed4 _ObjectIndex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            fixed4 color;
        };

        half4 LightingDeferred_PrePass(inout SurfaceOutput s, half4 light)
        {
            half4 c;
            c.rgb = s.Albedo.rgb * light ;
            c.a = s.Alpha;
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = t * _Color * .25;
            o.Alpha = t.a;
            o.Emission = t * _Color * _ObjectIndex.z * t.a * 10;

            float sample_l;
            float sample_r;
            float sample_u;
            float sample_d;
            float x_vector;
            float y_vector;

            if (IN.uv_MainTex.x > 0) { sample_l = tex2D(_MainTex, IN.uv_MainTex - (_MainTex_TexelSize.x,0)).a; }
            else { sample_l = t.a; }
            if (IN.uv_MainTex.x < 1) { sample_r = tex2D(_MainTex, IN.uv_MainTex + (_MainTex_TexelSize.x,0)).a; }
            else { sample_r = t.a; }
            if (IN.uv_MainTex.y > 0 ) { sample_u = tex2D(_MainTex, IN.uv_MainTex - (0,_MainTex_TexelSize.y)).a; }
            else { sample_u = t.a; }
            if (IN.uv_MainTex.y < 1) { sample_d =  tex2D(_MainTex, IN.uv_MainTex + (0,_MainTex_TexelSize.y)).a; }
            else { sample_d = t.a ;}
            x_vector = (((sample_l - sample_r)*(_ObjectIndex.x) + 1) * .5f) ;
            y_vector = (((sample_u - sample_d)*(_ObjectIndex.x) + 1) * .5f) ;
            
            o.Normal = UnpackNormal((1,x_vector,y_vector));
        }
        ENDCG

        Cull Front
        Lighting On
        ZTest LEqual
        ZWrite On

        CGPROGRAM
        #pragma surface surf Deferred alphatest:_Cutout
        #include "UnityLightingCommon.cginc"

        sampler2D _MainTex;
        fixed _BackfaceColor;
        float4 _MainTex_TexelSize;
        fixed4 _ObjectIndex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            fixed4 color;
        };

        half4 LightingDeferred_PrePass(inout SurfaceOutput s, half4 light)
        {
            half4 c;
            c.rgb = s.Albedo.rgb * light ;
            c.a = s.Alpha;
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo =  _BackfaceColor;
            o.Alpha = t.a;
            
            float sample_l;
            float sample_r;
            float sample_u;
            float sample_d;
            float x_vector;
            float y_vector;

            if (IN.uv_MainTex.x > 0) { sample_l = tex2D(_MainTex, IN.uv_MainTex - (_MainTex_TexelSize.x,0)).a; }
            else { sample_l = t.a; }
            if (IN.uv_MainTex.x < 1) { sample_r = tex2D(_MainTex, IN.uv_MainTex + (_MainTex_TexelSize.x,0)).a; }
            else { sample_r = t.a; }
            if (IN.uv_MainTex.y > 0 ) { sample_u = tex2D(_MainTex, IN.uv_MainTex - (0,_MainTex_TexelSize.y)).a; }
            else { sample_u = t.a; }
            if (IN.uv_MainTex.y < 1) { sample_d =  tex2D(_MainTex, IN.uv_MainTex + (0,_MainTex_TexelSize.y)).a; }
            else { sample_d = t.a ;}
            x_vector = (((sample_l - sample_r)*(_ObjectIndex.x) + 1) * .5f) ;
            y_vector = (((sample_u - sample_d)*(_ObjectIndex.x) + 1) * .5f) ;
            
            o.Normal = UnpackNormal((1,x_vector,y_vector));
        }
        ENDCG
    }



}