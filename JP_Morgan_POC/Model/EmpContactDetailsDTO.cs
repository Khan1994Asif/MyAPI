using System.ComponentModel.DataAnnotations;

namespace JP_Morgan_POC.Model
{
    public class EmpContactDetailsDTO
    {
        [Required]
        [StringLength(200)]
        [SalesforceType("string")]
        public string EmpAddress { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpProfile { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpFirstName { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpLastName { get; set; }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SalesforceTypeAttribute : Attribute
    {
        public string TargetType { get; }
        public SalesforceTypeAttribute(string targetType) { TargetType = targetType; }
    }

    // C# representations matching Salesforce Describe API structures

}
