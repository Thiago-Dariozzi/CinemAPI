using System;

namespace Domain.Entities;

public class SeatCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceMultiplier { get; set; } 
}