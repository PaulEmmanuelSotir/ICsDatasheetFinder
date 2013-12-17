﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace ICsDatasheetFinder_8._1.Common
{
    public class IncrementalLoadingDatasheetList : IncrementalLoadingBase
    {
        public IncrementalLoadingDatasheetList(List<Data.Part> parts, List<Data.Manufacturer> manufacturers)
        {
            DatasheetList = parts;
            Manufacturers = manufacturers;
        }

        protected async override Task<IList<object>> LoadMoreItemsOverrideAsync(System.Threading.CancellationToken c, uint count)
        {
            uint ToDo = System.Math.Min((uint)count, (uint)DatasheetList.Count - doneCount);
            var rslt = DatasheetList.GetRange((int)doneCount, (int)ToDo).ToList<object>();
            foreach(Data.Part part in rslt)
            {
                part.ManufacturerName = Manufacturers.Find(manu => manu.Id == part.ManufacturerId).name;
            }
            doneCount += ToDo;
            return rslt;
        }

        protected override bool HasMoreItemsOverride()
        {
            return DatasheetList.Count > doneCount;
        }

        protected uint doneCount = 0;
        List<Data.Part> DatasheetList;
        List<Data.Manufacturer> Manufacturers;
    }
}