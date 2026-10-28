using System.Runtime.InteropServices;
using Unity.Netcode;
using UnityEngine;

public class FogSimulation : MonoBehaviour {
    #region Fields
    [SerializeField] ComputeShader compute;
    [SerializeField] Material fogMaterial;
    [SerializeField] Transform player;
    [SerializeField] int resolution = 256;
    [SerializeField] float areaSize = 30f;
    [SerializeField] float playerRadius = 1.8f;
    [SerializeField] Vector2 wind = new(0.02f, 0.008f);
    [SerializeField, Range(0f, 10f)] float pushStrength = 4.5f;
    [SerializeField, Range(0f, 10f)] float swirlStrength = 1.5f;
    [SerializeField, Range(0f, 2f)] float regrowRate = 0.25f;
    
    RenderTexture[] density;
    int current, kernel, groups;
    Vector3 previousPlayerPos;

    public struct ObstacleData {
        public Vector2 position;
        public float radius;
    }
    
    ComputeBuffer obstacleBuffer;
    #endregion

    void Start() {
        ResolveLocalPlayer();

        // Only keep this fog simulation if the local player is within its area.
        // Each client runs both fogA and fogB, but should only simulate the one
        // that wraps around its own player.
        if (player != null)
        {
            float distX = Mathf.Abs(player.position.x - transform.position.x);
            if (distX > areaSize)
            {
                // Local player is in the other timeline — disable this fog
                enabled = false;
                var rend = GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = false;
                return;
            }
        }

        kernel = compute.FindKernel("CSMain");
        groups = Mathf.CeilToInt(resolution / 8f);
        density = new RenderTexture[2];

        for (int i = 0; i < 2; i++) {
            density[i] = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat) {
                enableRandomWrite = true,
                wrapMode = TextureWrapMode.Clamp
            };
            density[i].Create();
        }

        previousPlayerPos = player != null ? player.position : transform.position;
        InitializeObstacles();

        // Prime the field so we start with fully grown fog instead of an empty texture
        for (int i = 0; i < 90; i++) Step(0.1f);
    }

    void Update() {
        if (player == null)
        {
            ResolveLocalPlayer();
            if (player == null) return;
        }
        Step(Time.deltaTime);
    }

    /// <summary>
    /// The serialized "player" reference points at the player prefab asset, not the
    /// runtime instance (players are spawned by Netcode). A prefab asset reference
    /// loads non-null but lives outside any scene — detect that and resolve the
    /// local player object instead.
    /// </summary>
    void ResolveLocalPlayer() {
        if (player != null && player.gameObject.scene.IsValid()) return;
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;
        var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (playerObj != null)
            player = playerObj.transform;
    }

    void OnDestroy() {
        obstacleBuffer?.Release();
        if (density == null) return;
        foreach (var rt in density) rt.Release();
    }

    void InitializeObstacles() {
        var obstacles = FindObjectsByType<FogObstacle>(FindObjectsSortMode.None);
        var data = new ObstacleData[obstacles.Length];

        for (int i = 0; i < obstacles.Length; i++) {
            data[i].position = WorldToUv(obstacles[i].transform.position);
            data[i].radius = obstacles[i].Radius / areaSize;
        }
        
        obstacleBuffer = new ComputeBuffer(Mathf.Max(1, data.Length), Marshal.SizeOf(typeof(ObstacleData)));
        obstacleBuffer.SetData(data);
        compute.SetInt("_ObstacleCount", data.Length);
        compute.SetBuffer(kernel, "_Obstacles", obstacleBuffer);
    }

    void Step(float dt) {
        int next = 1 - current;
        Vector2 playerUv = WorldToUv(player.position);
        Vector2 playerVelUv = dt > 0f ? (playerUv - WorldToUv(previousPlayerPos)) / dt : Vector2.zero;
        
        compute.SetInt("_Resolution", resolution);
        compute.SetVector("_PlayerPos", playerUv);
        compute.SetVector("_PlayerVel", playerVelUv);
        compute.SetFloat("_PlayerRadius", playerRadius / areaSize);
        compute.SetVector("_Wind", wind);
        compute.SetFloat("_Push", pushStrength);
        compute.SetFloat("_Swirl", swirlStrength);
        compute.SetFloat("_Regrow", regrowRate);
        compute.SetFloat("t", Time.time);
        compute.SetFloat("dt", dt);
        compute.SetTexture(kernel, "Source", density[current]);
        compute.SetTexture(kernel, "Result", density[next]);
        compute.Dispatch(kernel, groups, groups, 1);
        
        fogMaterial.SetTexture("_DensityTex", density[next]);
        current = next;
        previousPlayerPos = player.position;
    }

    Vector2 WorldToUv(Vector3 worldPos) {
        Vector3 local = worldPos - transform.position;
        return new Vector2(local.x / areaSize + 0.5f, local.z / areaSize + 0.5f);
    }
}