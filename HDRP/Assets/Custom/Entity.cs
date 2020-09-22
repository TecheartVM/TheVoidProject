using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] protected float maxHealth;

    [SerializeField] private float _health;

    public bool isInvulnerable = false;

    public event EventHandler onHealthChanged;
    public event Action onDeath;

    public float health
    {
        get
        {
            return this._health;
        }
        private set
        {
            if (isDead) return;
            this._health = Mathf.Clamp(value, 0, maxHealth);
            if (this._health <= 0) Kill();
            if (onHealthChanged != null) onHealthChanged(this, EventArgs.Empty);
        }
    }

    public bool isDead { get; private set; } = false;


    void Awake()
    {
        setHealth();
    }

    public float getHealth()
    {
        return this.health;
    }

    public void setHealth()
    {
        this.health = this.maxHealth;
    }

    public virtual void DealDamage(float amount)
    {
        if(!isInvulnerable) this.health -= amount;
    }

    public void Heal(float healthIncrement)
    {
        this.health += healthIncrement;
    }

    public virtual void Kill()
    {
        this.isDead = true;
        if (onDeath != null) onDeath();
    }
}
