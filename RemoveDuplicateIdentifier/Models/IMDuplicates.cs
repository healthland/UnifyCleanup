using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveDuplicateIdentifier.Models
{
    internal class IMDuplicates
    {
        public string OrganizationId { get; set; } = string.Empty;
        public string IdentifierValue { get; set; } = string.Empty;
        public string Count { get; set; } = string.Empty;
    }
}
