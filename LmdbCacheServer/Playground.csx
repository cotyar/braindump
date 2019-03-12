#r "Spreads.LMDB"

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spreads.Buffers;
using Spreads.LMDB;


var DbPath = "./db/test.db";

var env = LMDBEnvironment.Create(DbPath);
env.Open();
var stats = env.GetStat();

Console.WriteLine($"Database opened. DB Stats: {stats}");
Console.WriteLine("entries: " + stats.ms_entries);
Console.WriteLine("MaxKeySize: " + env.MaxKeySize);
Console.WriteLine("ReaderCheck: " + env.ReaderCheck());


