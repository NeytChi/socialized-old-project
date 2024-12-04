using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Users
{
    public partial class Country : BaseEntity
    {
        [Column("name", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string Name { get; set; }
		[Column("fullname", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string Fullname { get; set; }
		[Column("english", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string English { get; set; }
		[Column("location", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string Location { get; set; }
		[Column("location_precise", TypeName="varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci")]
        public string LocationPrecise { get; set; }
    }
}