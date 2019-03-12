using LightningDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LmdbLight
{
    class Program
    {
        private static LightningDatabase CreateLightningDatabase(LightningEnvironment env, string name)
        {
            LightningDatabase db;
            using (var tx = env.BeginTransaction())
            {
                db = tx.OpenDatabase(name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });
                tx.Commit();
            }

            return db;
        }

        public static void DbWrite()
        {
            using (var env = new LightningEnvironment("db"))
            {
                env.MaxDatabases = 20;
                env.MapSize = 20L * 1024 * 1024 * 1024;
                env.Open(EnvironmentOpenFlags.NoThreadLocalStorage /*| EnvironmentOpenFlags.NoLock */ );

                using (var db = CreateLightningDatabase(env, "custom9"))
                {
                    Task runner = Task.Factory.StartNew(() => RunParallelRead(db, "outer", 10, 30), TaskCreationOptions.LongRunning);
                    using (var tx = env.BeginTransaction())
                    {
                        Console.WriteLine("Write opened");
                        for (int i = 0; i < 1000; i++)
                        {
                            tx.Put(db, Encoding.UTF8.GetBytes($"hello1 + {i}"), Encoding.UTF8.GetBytes("world"));
                        }

                        tx.Commit();

                        Console.WriteLine("Write finished");

                        Thread.Sleep(1000);

                        ReadBlock(db, "main");
                    }
                    runner.Wait();
                }
            }
        }

        private static void ReadBlock(LightningDatabase db, string name)
        {
            using (var tx = db.Environment.BeginTransaction(TransactionBeginFlags.ReadOnly))
            {
                Console.WriteLine($"{name} - Read opened");

                for (int i = 0; i < 10000000; i++)
                {
                    var result = tx.Get(db, Encoding.UTF8.GetBytes("hello"));
                }

                Console.WriteLine($"{name} - Read finished");
            }
        }

        private static void RunParallelRead(LightningDatabase db, string runPrefix, int threads, int count)
        {
            Enumerable.Range(0, count).
                AsParallel().WithDegreeOfParallelism(threads).
                ForAll(i => ReadBlock(db, $"par - {runPrefix} - {i}"));
        }

        static void Main(string[] args)
        {
            DbWrite();
            Console.WriteLine("Finished");
            Console.ReadLine();
            Console.WriteLine("Exiting");
        }
    }
}

