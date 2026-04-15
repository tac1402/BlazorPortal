using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BlazorWorld.Core.Entities.Content
{
    public class NodeCustomFields
    {
        [Key]
        public string? Id { get; set; }
        [ForeignKey("Node")]
        public string? NodeId { get; set; }
        public string? CustomField1 { get; set; }
        public string? CustomField2 { get; set; }
        public string? CustomField3 { get; set; }
        public string? CustomField4 { get; set; }
        public string? CustomField5 { get; set; }
        public string? IndexedCustomField1 { get; set; }
        public string? IndexedCustomField2 { get; set; }
        public string? IndexedCustomField3 { get; set; }
        public string? IndexedCustomField4 { get; set; }
        public string? IndexedCustomField5 { get; set; }
    }
}
