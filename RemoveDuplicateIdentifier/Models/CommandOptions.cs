using System.ComponentModel.DataAnnotations;
using CommandLine;


namespace RemoveDuplicateIdentifier.Models
{
    public class CommandOptions
    {    
            [Option('e', "environment", HelpText = "Environment: DEV, QA, STG, PD")]
            [Required]
            public string Environment { get; set; } = string.Empty;        
    }

}
