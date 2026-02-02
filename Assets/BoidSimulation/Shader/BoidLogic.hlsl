#ifndef BOID_LOGIC_INCLUDED
#define BOID_LOGIC_INCLUDED

struct BoidData 
{
    float3 position;
    float3 velocity;
};

StructuredBuffer<BoidData> _BoidBuffer;

float4x4 GetLookMatrix(float3 dir, float3 pos) 
{
    float3 forward = normalize(dir);
    if (length(dir) < 0.001) 
        forward = float3(0, 0, 1);
    
    float3 up = float3(0, 1, 0);
    if (abs(dot(forward, up)) > 0.99)
        up = float3(0, 0, 1);
    
    float3 right = normalize(cross(up, forward));
    up = cross(forward, right);
    
    return float4x4(
        right.x, up.x, forward.x, pos.x,
        right.y, up.y, forward.y, pos.y,
        right.z, up.z, forward.z, pos.z,
        0      , 0   , 0        , 1
    );
}

void BoidTransform_float(float3 InVertex, float BoidID, out float3 OutPosition) 
{
    uint id = (uint)BoidID;
    id = id % 50000; 

    BoidData data = _BoidBuffer[id];
    float4x4 mat = GetLookMatrix(data.velocity, data.position);
    float4 localPos = float4(InVertex, 1.0);
    float4 worldPos = mul(mat, localPos);
    OutPosition = worldPos.xyz;
}

void BoidTransform_half(half3 InVertex, float BoidID, out half3 OutPosition) 
{
    float3 v = InVertex;
    float3 o;
    BoidTransform_float(v, (float)BoidID, o);
    OutPosition = (half3)o;
}

#endif