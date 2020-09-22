using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EntityPlayer : Entity
{
    [SerializeField] private Text healthDisplayField;

    private void Start()
    {
        UpdateHealthBar(this, EventArgs.Empty);
        onHealthChanged += UpdateHealthBar;
    }

    public void UpdateHealthBar(object sender, EventArgs e)
    {
        healthDisplayField.text = $"{health}/{maxHealth} ♥";
    }
}
