using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoLProject.Persistence.Models;

public class ChampionStat
{
    [Key]
    public int Id { get; set; }

    // Clé étrangère vers le Champion
    public int ChampionId { get; set; }
    
    [ForeignKey("ChampionId")]
    public Champion Champion { get; set; } = null!;

    // Stats de base (Niveau 1)
    public double Hp { get; set; }
    public double HpPerLevel { get; set; }
    
    public double Mp { get; set; } // Mana
    public double MpPerLevel { get; set; }
    
    public double MoveSpeed { get; set; }
    
    public double Armor { get; set; }
    public double ArmorPerLevel { get; set; }
    
    public double SpellBlock { get; set; } // Résistance Magique
    public double SpellBlockPerLevel { get; set; }
    
    public double AttackRange { get; set; }
    
    public double HpRegen { get; set; }
    public double HpRegenPerLevel { get; set; }
    
    public double MpRegen { get; set; }
    public double MpRegenPerLevel { get; set; }
    
    public double Crit { get; set; }
    public double CritPerLevel { get; set; }
    
    public double AttackDamage { get; set; }
    public double AttackDamagePerLevel { get; set; }
    
    public double AttackSpeedPerLevel { get; set; }
    public double AttackSpeed { get; set; }
}