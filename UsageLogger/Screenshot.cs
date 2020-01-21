using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsageLogger
{
    class Screenshot
    {
        public int Id { get; set; }

        public int WindowLog_Id { get; set; }

        public byte[] Image { get; set; }
    }
}
