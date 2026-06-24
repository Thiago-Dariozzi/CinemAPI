using System;

namespace Domain.Entities;

public class Fee //la cometa 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PercentageRate { get; set; }     
    public decimal FixedAmount { get; set; }
}