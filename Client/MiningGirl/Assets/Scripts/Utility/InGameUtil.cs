using Data;
using UnityEngine;

public struct CalcPlayerDamage
{
    public float Damage { get; set; }

    public CalcPlayerDamage(int level, int str, int dex)
    {
        var baseDamage = 10f;
        var growthRate = 0.05f;
        var statDamage = str * Mathf.Pow(1f + growthRate, level - 1);
     
        Damage =  baseDamage + statDamage;
    }
}

public struct CalcPlayerStat
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Luk { get; set; }
    public float Damage { get; set; }
    public float Speed { get; set; }
    
    public CalcPlayerStat(int level, PlayerStatTable row)
    {
        var baseDamage = 10f;
        var baseSpeed = 2f;
        var statGrowthRate = 0.02f;
        var damageGrowthRate = 0.02f;

        if (level <= 1)
        {
            Str = row.Str;
            Dex = row.Dex;
            Luk = row.Luk;
            Damage = baseDamage;
            Speed = baseSpeed;
        }
        else
        {
            Str = row.Str + (int)(row.Str * Mathf.Pow(1f + row.StrGrowthRate * statGrowthRate, level) - row.Str);
            Dex = row.Dex + (int)(row.Dex * Mathf.Pow(1f + row.DexGrowthRate * statGrowthRate, level) - row.Dex);
            Luk = row.Luk + (int)(row.Luk * Mathf.Pow(1f + row.LukGrowthRate * statGrowthRate, level) - row.Luk);
            Damage =  baseDamage + Str * Mathf.Pow(1f + damageGrowthRate, level - 1);
            
            var speed = baseSpeed - (level * 0.01f) - (Dex * 0.005f);
            
            Speed = Mathf.Max(speed, 0.0f);
        }
    }
}