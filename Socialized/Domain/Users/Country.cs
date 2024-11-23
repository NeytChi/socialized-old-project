using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Users
{
    public partial class Country
    {
        [Key]
        [Column("country_id", TypeName="int(11)")]
        public int countryId { get; set; }
        [Column("name", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string name { get; set; }
		[Column("fullname", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string fullname { get; set; }
		[Column("english", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string english { get; set; }
		[Column("alpha2", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string alpha2 { get; set; }
		[Column("alpha3", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string alpha3 { get; set; }
		[Column("iso", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string iso { get; set; }
		[Column("location", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string location { get; set; }
		[Column("location_precise", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string location_precise { get; set; }
    }
}