using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP;

    [Header("Events")]
    public UnityEvent onDamage;
    public UnityEvent onDeath;
    public UnityEvent onHeal;

    void Awake()
    {
        currentHP = maxHP;
    }

    // �ѧ��ѹ�Ѻ�����
    public void TakeDamage(int dmg)
    {
        if (dmg <= 0) return;

        currentHP -= dmg;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        onDamage?.Invoke();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // �ѧ��ѹ Heal
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        onHeal?.Invoke();
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        onDeath?.Invoke();
        // ����ö���͡����� GameObject ��ç���
        // Destroy(gameObject);
    }

    // Helper ����� HP ��������ѧ
    public bool IsDead()
    {
        return currentHP <= 0;
    }
}