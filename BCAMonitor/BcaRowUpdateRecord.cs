using System;

namespace BCAMonitor
{
    internal class BcaRowUpdateRecord
    {
        public string PlanRefNumber { get; set; }
        public string ApplicationType { get; set; }
        public string Status { get; set; }
        public DateTime DateTime { get; set; }
        public string ViewDetails { get; set; }
        public string OldPlanRefNumber { get; set; }
        public string OldApplicationType { get; set; }
        public string OldStatus { get; set; }
        public DateTime OldDateTime { get; set; }
        public string OldViewDetails { get; set; }
    }
}