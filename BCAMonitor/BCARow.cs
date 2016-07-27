using System;
namespace BCAMonitor
{
    public class BcaRow
    {
        public string PlanRefNumber { get; set; }
        public string ApplicationType { get; set; }
        public string Status { get; set; }
        public DateTime DateTime { get; set; }
        public string ViewDetails { get; set; }
    }
}