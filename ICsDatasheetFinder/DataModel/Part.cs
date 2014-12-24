using SQLite;
using System.Collections.Generic;

namespace ICsDatasheetFinder.Data
{
	public class Part : IEqualityComparer<Part>
	{
		public bool Equals(Part p1, Part p2) => p1.reference == p2.reference;

		public int GetHashCode(Part p) => p.reference.GetHashCode();

		[PrimaryKey]
		public string reference { get; set; }

		[Indexed]
		public int ManufacturerId { get; set; }

		public string ManufacturerName { get; set; }

		[MaxLength(500)]
		public string datasheetURL { get; set; }
	}
}