using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LightningDB;
//using Spreads.LMDB;

namespace LmdbCacheServer
{
    public class Lmdb
    {
        public LightningEnvironment Environment { get; private set; }

        private IDictionary<string, LightningDatabase> _databases =
            new ConcurrentDictionary<string, LightningDatabase>();

        public Lmdb(string path, int maxDatabases)
        {
            Environment = new LightningEnvironment(path) { MaxDatabases = 2 };
            Environment.Open(EnvironmentOpenFlags.NoThreadLocalStorage);
        }

        // private void WriteAsync(Action) 

        //public async LightningDatabase OpenDatabase(string name)
        //{
        //    if (_databases.Select(d => d.Name == name)
        //    {
        //        return 
        //    }
        //}
    }
}
