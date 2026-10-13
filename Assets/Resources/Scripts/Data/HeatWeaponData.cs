using UnityEngine;

[CreateAssetMenu(fileName = "NewHeatWeapon", menuName = "Dark Horizon/Weapons/Heat Weapon")]
public class HeatWeaponData : WeaponData
{
    [Header("Calor")]
    [Tooltip("Calor maximo antes de sobrecalentarse.")]
    [SerializeField] private float maxHeat = 100f;
    [Tooltip("Calor que suma cada disparo.")]
    [SerializeField] private float heatPerShot = 8f;
    [Tooltip("Calor que pierde por segundo al no disparar.")]
    [SerializeField] private float coolingPerSecond = 25f;
    [Tooltip("Al sobrecalentarse, no vuelve a disparar hasta bajar a este calor.")]
    [SerializeField] private float resumeAtHeat = 30f;

    public float MaxHeat => maxHeat;
    public float HeatPerShot => heatPerShot;
    public float CoolingPerSecond => coolingPerSecond;
    public float ResumeAtHeat => resumeAtHeat;

}               
