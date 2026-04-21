using System.ComponentModel.DataAnnotations;

public class ClaimSearchFilter
{
    [MaxLength(20, ErrorMessage = "Max 20 characters allowed")]
    [MinLength(2, ErrorMessage = "At least 2 characters required")]
    [Display(Name = "provider")]
    public string ProvId { get; set; }

    [MaxLength(15, ErrorMessage = "Max 15 characters allowed")]
    [Display(Name = "member")]
    public string MemId { get; set; }

    public string ClaimId { get; set; }

    public int Skip { get; set; }
    public int Take { get; set; }

    public bool IsToUpdate { get; set; }
}