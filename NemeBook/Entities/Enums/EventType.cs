using System.ComponentModel.DataAnnotations;

namespace Entities.Enums;

public enum EventType
{
    [Display(Name = "Тест")]
    Test,
    
    [Display(Name = "Домашна работа")]
    Homework,
    
    [Display(Name = "Екскурзия")]
    Trip
}