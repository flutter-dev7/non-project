using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Reports.Request;

public class SalesReportRequest
{
    [Required]
    public DateOnly? From { get; set; }

    [Required]
    public DateOnly? To { get; set; }
}