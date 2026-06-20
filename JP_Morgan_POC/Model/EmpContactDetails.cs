using System.ComponentModel.DataAnnotations;

namespace JP_Morgan_POC.Model
{
    public class EmpContactDetails
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string EmpAddress { get; set; }

        [StringLength(100)]
        public string EmpProfile { get; set; }

        [StringLength(500)]
        public string SynStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        //public string MobilePhone { get; set; }
        [StringLength(100)]
        public string EmpFirstName { get; set; }

        [StringLength(100)]
        public string EmpLastName { get; set; }

    }
}
