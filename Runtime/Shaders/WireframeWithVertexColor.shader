Shader "MeshLib/Wireframe With Vertex Colors"
{
    Properties
    {
        _Thickness("Thickness", Range(0.001, 0.2)) = 0.02
        _FrontColor("Wireframe Color", color) = (0.0, 0.0, 1.0, 1.0)
    }
        SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex VSMain
            #pragma geometry GSMain
            #pragma fragment PSMain
            #pragma target 4.0

            float _Thickness;
            float4 _FrontColor;

            struct Data
            {
                float4 vertex : SV_Position;
                float2 barycentric : BARYCENTRIC;
                float4 color : COLOR;
            };

            void VSMain(inout float4 vertex:POSITION, inout float4 color : COLOR) { }

            [maxvertexcount(3)]
            void GSMain(triangle float4 patch[3]:SV_Position, triangle float4 color[3] : COLOR, inout TriangleStream<Data> stream)
            {
                Data GS;
                for (uint i = 0; i < 3; i++)
                {
                    GS.vertex = UnityObjectToClipPos(patch[i]);
                    GS.barycentric = float2(fmod(i,2.0), step(2.0,i));
                    GS.color = color[i];
                    stream.Append(GS);
                }
                stream.RestartStrip();
            }

            float4 PSMain(Data PS) : SV_Target
            {
                float3 coord = float3(PS.barycentric, 1.0 - PS.barycentric.x - PS.barycentric.y);
                coord = smoothstep(fwidth(coord) * 0.1, fwidth(coord) * 0.1 + fwidth(coord), coord);
                return float4(lerp(_FrontColor, PS.color, min(coord.x, min(coord.y, coord.z)).xxx), 1.0);
            }
            ENDCG
        }
    }
}