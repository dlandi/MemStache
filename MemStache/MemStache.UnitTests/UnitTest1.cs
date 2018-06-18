using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemStache;
using System.Diagnostics;
using System;


//using Stache = MemStache.MemStache<string, MemStache.IMemStacheProtectedItem<string>>;
using Stache = MemStache.MemStache<string, MemStache.MemStacheProtectedItem<string>>;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;

namespace MemStache.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private TestContext testContext;

        private Stopwatch stopWatch;

        //public MemStache<string, MemStache.IMemStacheProtectedItem<string>> stash ;
        public MemStache<string, MemStache.MemStacheProtectedItem<string>> stash;


        #region Built-in Test Events

        //[AssemblyInitialize()]
        public static void AssemblyInit(TestContext testContext)
        {
            testContext.WriteLine("AssemblyInit " + testContext.TestName);
        }

        /// <summary>
        /// see MSDN for sequence of built-in Test Events:
        /// http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.testtools.unittesting.classinitializeattribute(v=vs.110).aspx
        /// </summary>
        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {


            //testContext.WriteLine("ClassInit " + testContext.TestName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            //testContext.WriteLine("TestMethodInit: " + testContext.TestName);

            stopWatch = new Stopwatch();



            stopWatch.Start();
        }

        [TestCleanup()]
        public void Cleanup()
        {
            //testContext.WriteLine("TestMethodCleanup: " + testContext.TestName);

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);

            Console.WriteLine("TIME ELAPSED: " + elapsedTime);
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            //testContext.WriteLine("ClassCleanup");
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            //testContext.WriteLine("AssemblyCleanup");
        }

        #endregion

        /*
                [TestMethod]
                public void InitMemStache()
                {
                    try
                    {
                        stash =
                        Stache.Create<string>(StacheItemEnum.stchProtected,
                                      //The Setter Lambda
                                      async (key, data) =>
                                      {
                                          await Task.Delay(1000);
                                          string s = string.Format("MemStacheItem Setter: {0}",
                                                                    DateTime.UtcNow.ToLongTimeString());
                                          Console.WriteLine("Save Data: {0}", s);

                                      },
                                      //The Getter Lambda
                                      async (key) =>
                                      {
                                          await Task.Delay(1000);
                                          string s = string.Format("MemStacheItem Getter - Key: {0}; Value: {1}",
                                                                   key, DateTime.UtcNow.ToLongTimeString());
                                          Console.WriteLine("Get Data: {0}", s);
                                          if (key != null)
                                              return s;

                                          return null;
                                      }
                  );

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: {0}"+e.Message);
                        //throw;
                    }

                    Console.WriteLine("Success: MemStache Created");
                }
                [TestMethod]
                public void RegisterMemCacheItemInfo()
                {
                    var stash =
                    Stache.Create<string>(StacheItemEnum.stchProtected,
                                  //The Setter Lambda
                                  async (key, data) =>
                                  {
                                      await Task.Delay(1000);
                                      string ss = string.Format("MemStacheItem Setter: {0}",
                                                                DateTime.UtcNow.ToLongTimeString());
                                      Console.WriteLine("Save Data: {0}", ss);

                                  },
                                  //The Getter Lambda
                                  async (key) =>
                                  {
                                      await Task.Delay(1000);
                                      string s1 = string.Format("MemStacheItem Getter - Key: {0}; Value: {1}",
                                                               key, DateTime.UtcNow.ToLongTimeString());
                                      Console.WriteLine("Get Data: {0}", s1);
                                      if (key != null)
                                          return s1;

                                      return null;
                                  }
                                  );

                    string k = "test01";
                    stash.RegisterItem(
                                    new MemStacheItemInfo<string>(
                                        k,
                                        DateTimeOffset.Now,
                                                  //The Setter Lambda
                                                  async (key, data) =>
                                                  {
                                                      await Task.Delay(1000);
                                                      string s2 = string.Format("StacheItemInfo Setter: {0}",
                                                                                DateTime.UtcNow.ToLongTimeString());
                                                      Console.WriteLine("Save Data: {0}", s2);

                                                  },
                                                  //The Getter Lambda
                                                  async (key) =>
                                                  {
                                                      await Task.Delay(1000);
                                                      string s4 = string.Format("StacheItemInfo Getter - Key: {0}; Value: {1}",
                                                                               key, DateTime.UtcNow.ToLongTimeString());
                                                      Console.WriteLine("Get Data: {0}", s4);
                                                      if (key != null)
                                                          return s4;

                                                      return null;
                                                  }
                                            )                                
                                        );

                    //var stch = stash as Stache;
                    stash.GetOrAdd(k).Wait();


                }
        */

        [TestMethod]
        public void CreateMemStache()
        {
            StacheMeister Meister = new StacheMeister("memstache.demo");
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("Purpose: {0}", stash.Purpose);
        }

        [TestMethod]
        public void TestSerialization()
        {
            string key = "test01";
            StacheMeister Meister = new StacheMeister("memstache.demo");
            var rowcount = Meister.DB.Delete<Stash>(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            var payload = new Stash() { key = key, Object = "This is a test" };

            var hash1 = Stasher.Hash(payload.value);

            stash[key] = payload;

            payload.value = "";

            payload = stash[key];

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1, hash2);

        }

        [TestMethod]
        public void TestDBInsert()
        {
            string key = "test02";
            StacheMeister Meister = new StacheMeister("memstache.demo");
            var rowcount = Meister.DB.Delete<Stash>(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            string s = "another test";
            s = JsonConvert.SerializeObject(s);

            stash.DbAddOrUpdate(new Stash() { key = key, value = s, serialized = true });
            //stash.DB.Insert(new Stash() { key = key,  value = s, serialized=true });
            Task.Delay(1000);
            Stash result = stash.DbGet(key);
            Console.WriteLine("Payload Test: {0}", result.value);
        }






        public class Stock
        {
            public int Id { get; set; } = 1;
            public string Symbol { get; set; } = "LANDI";
        }

        public class Valuation
        {
            public int Id { get; set; } = 100;
            public int StockId { get; set; } = 1;
            public DateTime Time { get; set; } = DateTime.UtcNow;
            public decimal Price { get; set; } = 100.34m;
        }

        [TestMethod]
        public void TestSerialization2()
        {
            string key = "test03";
            StacheMeister Meister = new StacheMeister("memstache.demo");
            var rowcount = Meister.DB.Delete<Stash>(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { key = key, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
                Console.WriteLine("Payload is nulls");


            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1, hash2);

        }
        [TestMethod]
        public void TestSerializeAndCompress()
        {
            string key = "test04";
            StacheMeister Meister = new StacheMeister("memstache.demo");
            var rowcount = Meister.DB.Delete<Stash>(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerializeCompress);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { key = key, stashPlan = StashPlan.spSerializeCompress, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
                Console.WriteLine("Payload is nulls");


            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1, hash2);

        }

        [TestMethod]
        public void TestSerializeAndCompressAndEncrypt()
        {
            string key = "test05";
            StacheMeister Meister = new StacheMeister("memstache.demo");
            var rowcount = Meister.DB.Delete<Stash>(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spProtectCompress);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { key = key, stashPlan = StashPlan.spProtectCompress, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
                Console.WriteLine("Payload is nulls");


            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1, hash2);

        }


        [TestMethod]
        public void StacheMeisterSerialization()
        {
            string key = "test06";
            StacheMeister Meister = new StacheMeister("memstache.demo", StashPlan.spSerialize);
            var rowcount = Meister.DB.Delete<Stash>(key);

            Valuation valuation1 = new Valuation();

            Meister[key] = valuation1;

            Valuation valuation2 = Meister[key] as Valuation;

            string v1 = JsonConvert.SerializeObject(valuation1);
            string v2 = JsonConvert.SerializeObject(valuation2);

            Assert.AreEqual(v1, v2);

        }
        [TestMethod]
        public void StacheMeisterSerializeAndCompress()
        {
            string key = "test07";
            StacheMeister Meister = new StacheMeister("memstache.demo", StashPlan.spSerializeCompress);
            var rowcount = Meister.DB.Delete<Stash>(key);

            Valuation valuation1 = new Valuation();

            Meister[key] = valuation1;

            Valuation valuation2 = Meister[key] as Valuation;

            string v1 = JsonConvert.SerializeObject(valuation1);
            string v2 = JsonConvert.SerializeObject(valuation2);

            Assert.AreEqual(v1, v2);

        }

        [TestMethod]
        public void StacheMeisterSerializeAndCompressAndEncrypt()
        {
            string key = "test08";
            StacheMeister Meister = new StacheMeister("memstache.demo", StashPlan.spProtectCompress);
            var rowcount = Meister.DB.Delete<Stash>(key);

            Valuation valuation1 = new Valuation();

            Meister[key] = valuation1;

            Valuation valuation2 = Meister[key] as Valuation;

            string v1 = JsonConvert.SerializeObject(valuation1);
            string v2 = JsonConvert.SerializeObject(valuation2);

            Assert.AreEqual(v1, v2);

        }


        public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
                public string State { get; set; }
                public string Country { get; set; }
        }

        public class Dept
        {
            public string Name { get; set; }
            public string Branch { get; set; }
            public int Division { get; set; }
            public string Country { get; set; }
            public bool Eligible { get; set; }

        }

        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Address Address { get; set; }
        }
        
        public class Employee
        {
            public int Id { get; set; }
            public Dept Department { get; set; }
            public Person Person { get; set; }
        }
        public Employee CreateEmployee()
        {
            return new Employee()
            {
                Id = 1,
                Department = new Dept()
                {
                    Branch = "Main",
                    Country = "USA",
                    Division = 1,
                    Eligible = true,
                    Name = "Sales"
                },
                Person = new Person()
                {
                    Name = "Sam Adams",
                    Age = 33,
                    Address = new Address()
                    {
                        Street = "123 Main Street",
                        City = "Ashburn",
                        State = "VA",
                        Country ="USA"
                    }
                }
            };
        }
        [TestMethod]
        public void TestObjectGraph()
        {
            string key = "test08";
            StacheMeister Meister = new StacheMeister("memstache.demo", StashPlan.spProtectCompress);
            var rowcount = Meister.DB.Delete<Stash>(key);

            Employee emp1 = CreateEmployee();
            string v1 = JsonConvert.SerializeObject(emp1);

            Meister[key] = emp1;

            Employee emp2 = Meister[key] as Employee;


            string v2 = JsonConvert.SerializeObject(emp2);

            Assert.AreEqual(v1, v2);
        }


    }
}
