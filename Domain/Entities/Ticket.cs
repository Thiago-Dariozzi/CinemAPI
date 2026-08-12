using System;

namespace Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid ScreenId { get; set; }
    public Guid UserId { get; set; }
    public DateTime BuyDate { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsActive { get; set; }   
}