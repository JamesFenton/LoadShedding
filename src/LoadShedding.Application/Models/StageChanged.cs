using System;
using System.Collections.Generic;
using System.Text;

namespace LoadShedding.Application.Models
{
    public class StageChanged
    {
        public int PreviousStage { get; set; }
        public int CurrentStage { get; set; }
    }
}
