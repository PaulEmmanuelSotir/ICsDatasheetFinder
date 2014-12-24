using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICsDatasheetFinder.Common
{
	public class IncrementalLoadingDatasheetList : IncrementalLoadingBase
	{
		public IncrementalLoadingDatasheetList(List<Data.Part> parts, List<Data.Manufacturer> manufacturers)
		{
			_datasheetList = parts;
			_manufacturers = manufacturers;
		}

		protected async override Task<IList<object>> LoadMoreItemsOverrideAsync(System.Threading.CancellationToken c, uint count)
		{
			uint ToDo = System.Math.Min((uint)count, (uint)_datasheetList.Count - _doneCount);
			var rslt = _datasheetList.GetRange((int)_doneCount, (int)ToDo);

			_doneCount += ToDo;

			await Task.Run(() =>
			{
				Parallel.ForEach(rslt, (part) =>
				{
					part.ManufacturerName = _manufacturers.Find(manu => manu.Id == part.ManufacturerId).name;
				});
			});

			return rslt.ToList<object>();
		}

		protected override bool HasMoreItemsOverride() => _datasheetList.Count > _doneCount;

		protected uint _doneCount = 0;
		protected readonly List<Data.Part> _datasheetList;
		protected readonly List<Data.Manufacturer> _manufacturers;
	}
}