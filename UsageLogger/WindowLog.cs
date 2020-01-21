using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UsageLogger
{
    class WindowLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public string ProgramName { get; set; }
    }
}
