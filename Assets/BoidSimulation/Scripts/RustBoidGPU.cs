using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace BoidSimulation.Scripts
{
    public class RustBoidGPU : MonoBehaviour
    {
        private const string DllName = "boid_simulation";

        [DllImport(DllName)]
        private static extern void initialize_boids(int count);

        [DllImport(DllName)]
        private static extern void update_boids(float deltaTime);

        [DllImport(DllName)]
        private static extern IntPtr get_boids_ptr();

        [DllImport(DllName)]
        private static extern void set_simulation_params(
            float speed,
            float perceptionRadius,
            float cohesionWeight,
            float alignmentWeight,
            float separationWeight
        );

        struct BoidData
        {
            public Vector3 position;
            public Vector3 velocity;
        }

        [Header("Settings")] 
        public int boidCount = 50000;
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public Vector3 boundsSize = new Vector3(500, 100, 500);

        [Header("Simulation Parameters")]
        [Range(0f, 20f)] public float speed = 5.0f;
        [Range(0.1f, 10f)] public float perceptionRadius = 2.0f;
        [Range(0f, 0.1f)] public float cohesionWeight = 0.01f;
        [Range(0f, 0.2f)] public float alignmentWeight = 0.05f;
        [Range(0f, 5.0f)] public float separationWeight = 2.5f;
        
        private ComputeBuffer boidBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        private void Start()
        {
            // Rust側でメモリ確保
            initialize_boids(boidCount);
            UpdateSimulationParams();

            // Boidデータのバッファ
            boidBuffer = new ComputeBuffer(boidCount, 24);
            
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            args[0] = (uint)instanceMesh.GetIndexCount(0);
            args[1] = (uint)boidCount;
            args[2] = (uint)instanceMesh.GetIndexStart(0);
            args[3] = (uint)instanceMesh.GetBaseVertex(0);
            argsBuffer.SetData(args);
            
            instanceMaterial.SetBuffer("_BoidBuffer", boidBuffer);
        }

        private unsafe void Update()
        {
            UpdateSimulationParams();
            
            // Rustで計算
            update_boids(Time.deltaTime);
            
            // ポインタを取得する
            IntPtr ptr = get_boids_ptr();
            if (ptr == IntPtr.Zero)
                return;

            // データをGPUに転送
            unsafe
            {
                // Rustのメモリ領域をNativeArrayとしてラップしておく
                var nativeArray = 
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BoidData>(
                        (void*)ptr,
                        boidCount,
                        Allocator.None
                    );
                
                // 安全ハンドルを設定
                #if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.Create());
                #endif
                
                // ここでVRAMに転送
                boidBuffer.SetData(nativeArray);
                
                if (Time.frameCount % 100 == 0)
                {
                    BoidData firstBoid = nativeArray[0];
                    Debug.Log($"Boid[0] Pos: {firstBoid.position}, Vel: {firstBoid.velocity}");
                }
            }
            
            // 描画
            Graphics.DrawMeshInstancedIndirect(
                instanceMesh,
                0,
                instanceMaterial,
                new Bounds(Vector3.zero, boundsSize),
                argsBuffer
            );
        }

        private void UpdateSimulationParams()
        {
            set_simulation_params(
                speed,
                perceptionRadius,
                cohesionWeight,
                alignmentWeight,
                separationWeight
            );
        }

        private void OnDestroy()
        {
            if (boidBuffer != null)
                boidBuffer.Release();
            
            if (argsBuffer != null)
                argsBuffer.Release();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        }
    }
}