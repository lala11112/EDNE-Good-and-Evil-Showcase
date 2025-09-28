using UnityEngine;


[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FireBall : MonoBehaviour, ISkillProjectile 
{
    [HideInInspector] public float damage;
    [HideInInspector] public float speed;
    public float lifeTime = 5f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void FireTowards(Transform target)
    {
        if (target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        

        
        if (other.gameObject.CompareTag("Player"))
        {
            
            PlayerHp hp = other.gameObject.GetComponent<PlayerHp>();
            

            if (hp != null)
            {
                hp.PlayerDecrease(Mathf.RoundToInt(damage));
                
            }

            Destroy(gameObject); 
        }
    }
   
    public void SetSkillData(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;
        
    }
    
} 