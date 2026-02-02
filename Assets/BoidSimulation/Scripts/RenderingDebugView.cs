using TMPro;
using UnityEngine;

namespace BoidSimulation.Scripts
{
    public class RenderingDebugView : MonoBehaviour
    {
        [Header("Dependency")]
        [SerializeField] private RustBoidGPU boidSimulation;
        
        [Header("Text Settings")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI boidCountText;
        
        [Header("FPS Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        
        private float accum = 0f;
        private int frames = 0;
        private float timeLeft;

        private void Start()
        {
            timeLeft = updateInterval;
        }

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            frames++;

            if (timeLeft <= 0f)
            {
                float fps = accum / frames;
                fpsText.text = $"FPS: {fps:F1}";
                boidCountText.text = $"Boids: {boidSimulation.boidCount}";
                
                timeLeft = updateInterval;
                accum = 0f;
                frames = 0;
            }
        }
    }
}