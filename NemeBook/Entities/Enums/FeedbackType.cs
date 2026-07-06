using System.ComponentModel.DataAnnotations;

namespace Entities.Enums;

public enum FeedbackType
{
    [Display(Name = "Похвала")]
    Praise,
    [Display(Name = "Забележка")]
    Remark
}