using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterStat
{
    [SerializeField] private float baseValue;
    public float value
    {
        get
        {
            if (isDirty)
            {
                SortModifiers();
                RecalculateMods();
            }
            return _value;
        }
    }

    private List<StatModifier> modifiers;
    private float _value;

    private bool isDirty = true;

    public CharacterStat()
    {
        modifiers = new List<StatModifier>();
    }
    public CharacterStat(float baseValue) : this()
    {
        this.baseValue = baseValue;
    }

    public float GetBaseValue()
    {
        return baseValue;
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        isDirty = true;
    }
    public bool RemoveModifier(StatModifier modifier)
    {
        if (modifiers.Remove(modifier))
        {
            isDirty = true;
            return true;
        }
        return false;
    }

    public bool RemoveModsBySource(object source)
    {
        bool itemsRemoved = false;
        for(int i = modifiers.Count - 1; i >= 0; i--)
        {
            if(modifiers[i].source == source)
            {
                modifiers.RemoveAt(i);
                itemsRemoved = true;
            }
        }
        if (itemsRemoved) isDirty = true;
        return itemsRemoved;
    }

    private void SortModifiers()
    {
        modifiers.Sort(StatModifier.CompareOrder);
    }

    public void RecalculateMods()
    {
        _value = baseValue;
        float sumPercentAdd = 0;

        for(int i = 0; i < modifiers.Count; i++)
        {
            StatModifier mod = modifiers[i];

            switch(mod.type)
            {
                case StatModType.Flat:
                    _value += mod.value;
                    break;
                case StatModType.PercentAdd:
                    if (mod.value < 0) break;
                    sumPercentAdd += mod.value;
                    if(i + 1 >= modifiers.Count || modifiers[i + 1].type != StatModType.PercentAdd)
                    {
                        _value *= 1 + sumPercentAdd;
                        sumPercentAdd = 0;
                    }
                    break;
                case StatModType.Mult:
                    if (mod.value < 0) break;
                    _value *= mod.value;
                    break;
                case StatModType.PercentMult:
                    if (mod.value < 0) break;
                    _value *= 1 + mod.value;
                    break;
            }
        }

        _value = (float)Math.Round(_value, 2);
        isDirty = false;
        Debug.Log(_value);
    }
}
