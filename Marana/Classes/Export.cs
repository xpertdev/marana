﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

namespace Marana {

    public class Export {

        public static void TSD_To_CSV(DatasetTSD dataset, string filepath) {
            using (StreamWriter sw = new StreamWriter(filepath)) {
                using (CsvWriter csv = new CsvWriter(sw, CultureInfo.InvariantCulture)) {
                    csv.Context.RegisterClassMap<DailyValueMap>();

                    csv.WriteRecords(dataset.TSDValues);
                }
            }
        }

        public class DailyValueMap : ClassMap<TSDValue> {

            public DailyValueMap() {
                Map(s => s.Timestamp).Index(0).Name("timestamp");
                Map(s => s.Open).Index(1).Name("open");
                Map(s => s.High).Index(2).Name("high");
                Map(s => s.Low).Index(3).Name("low");
                Map(s => s.Close).Index(4).Name("close");
                Map(s => s.Volume).Index(6).Name("volume");
                Map(s => s.SMA7).Index(9).Name("sma7");
                Map(s => s.SMA20).Index(10).Name("sma20");
                Map(s => s.SMA50).Index(11).Name("sma50");
                Map(s => s.SMA100).Index(12).Name("sma100");
                Map(s => s.SMA200).Index(13).Name("sma200");
                Map(s => s.MSD20).Index(14).Name("msd20");
                Map(s => s.MSDr20).Index(15).Name("msdr20");
                Map(s => s.vSMA20).Index(16).Name("vsma20");
                Map(s => s.vMSD20).Index(17).Name("vmsd20");
            }
        }
    }
}