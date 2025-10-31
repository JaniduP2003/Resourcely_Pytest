using System.ComponentModel.DataAnnotations;

namespace Backend_Resourcely.Dto
{
    public class DepartmentDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
