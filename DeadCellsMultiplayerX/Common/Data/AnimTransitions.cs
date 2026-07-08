using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class AnimTransitions
    {
        public string? Anim { get; set; } = string.Empty;
        public string? From { get; set; } = string.Empty;
        public string? To { get; set; } = string.Empty;
        public bool? reverse { get; set; } = null;
        public double? speed { get; set; } = null;
    }
}