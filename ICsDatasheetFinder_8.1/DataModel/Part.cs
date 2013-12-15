using SQLite;
using System.Collections.Generic;

namespace ICsDatasheetFinder_8._1.Data
{
    public class Part : IEqualityComparer<Part>
{
        public bool Equals(Part p1, Part p2)
        {
            return p1.reference == p2.reference;
        }

        public int GetHashCode(Part p)
        {
            return p.reference.GetHashCode();
        }

        [PrimaryKey]
        public string reference { get; set; }

        [Indexed]
        public int ManufacturerId { get; set; }

        [MaxLength(500)]
        public string datasheetURL { get; set; }
    }
}