using System.Collections.Generic;
using UnityEngine;

public class MinorEnemySpawner : MonoBehaviour
{
    [Header("Configuración del Spawner")]
    [SerializeField] private MinorEnemyType enemyTypeToSpawn = MinorEnemyType.TombBeetle;
    [SerializeField] private float spawnInterval = 7.5f;
    [SerializeField] private int maxConcurrentEnemies = 2; // Hasta 2 escarabajos por sala simultáneamente
    [SerializeField] private float spawnRadius = 1.0f;

    private float timer = 0f;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Sprite beetleSprite0;
    private Sprite beetleSprite1;

    private void Awake()
    {
        beetleSprite0 = Resources.Load<Sprite>("Enemies/beetle_walk_0");
        beetleSprite1 = Resources.Load<Sprite>("Enemies/beetle_walk_1");
    }

    private void OnEnable()
    {
        timer = 0f;
        // Spawn inmediato del primer enemigo al activarse el Despertar
        SpawnEnemy();
    }

    private void Update()
    {
        // Limpiar lista de enemigos muertos
        activeEnemies.RemoveAll(item => item == null);

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            if (activeEnemies.Count < maxConcurrentEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    public void SpawnEnemy()
    {
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

        GameObject enemy = new GameObject("Escarabajo_Tumba");
        enemy.transform.position = spawnPos;
        enemy.transform.localScale = new Vector3(0.35f, 0.35f, 1f); // Escarabajo pequeño

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = beetleSprite0 != null ? beetleSprite0 : Resources.Load<Sprite>("Enemies/beetle_walk_0");
        sr.sortingOrder = 8;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; // Radio accesible para impacto de bala
        col.isTrigger = true;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        MinorEnemyAI ai = enemy.AddComponent<MinorEnemyAI>();

        activeEnemies.Add(enemy);
    }
}
