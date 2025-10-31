namespace Backend_Resourcely.Dto
{
    public class BatchCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class BatchUpdateDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
