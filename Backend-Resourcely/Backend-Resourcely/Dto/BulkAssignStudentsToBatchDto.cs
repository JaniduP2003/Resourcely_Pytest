using System.Collections.Generic;

namespace Backend_Resourcely.Dto
{
    public class BulkAssignStudentsToBatchDto
    {
        public int BatchId { get; set; }
        public List<int> UserIds { get; set; } = new();
    }
}
