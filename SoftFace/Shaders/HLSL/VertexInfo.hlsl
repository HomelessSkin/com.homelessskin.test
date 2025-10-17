struct VertexInfo
{
    float3 Position;
    float3 Normal;
};

struct Head
{
    float4x4 objectToWorld;

    int RenderableID;
};

int _VertexLen;
StructuredBuffer<Head> _HeadData;
StructuredBuffer<VertexInfo> _VertexData;

void GetData_float (in int id, in int vertexIndex, out float3 position, out float3 normal)
{
    int index = _VertexLen * id + vertexIndex;
    
    position = _VertexData[index].Position;
    normal = _VertexData[index].Normal;
}