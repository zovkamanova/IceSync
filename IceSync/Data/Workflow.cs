using System.Diagnostics.Eventing.Reader;

namespace IceSync.Data
{
    public class Workflow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public string MultiExecBehavior { get; set; }
    }
}
