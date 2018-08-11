namespace MemStache.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using MemStache.LiteDB;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class UnitTest1
    {
        private ServiceCollection services = new ServiceCollection();
        private StacheMeister GlobalMeister;

        private TestContext testContext;

        private Stopwatch stopWatch;

        public Employee employee1;

        #region Built-in Test Events

        //[AssemblyInitialize()]
        public static void AssemblyInit(TestContext testContext)
        {
            testContext.WriteLine("AssemblyInit " + testContext.TestName);
        }

        /// <summary>
        /// see MSDN for sequence of built-in Test Events:
        /// http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.testtools.unittesting.classinitializeattribute(v=vs.110).aspx.
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
            employee1 = CreateEmployee();

            string appId, filename, password;
            appId = "memstache.demo";
            filename = "MemstacheTest.cv";
            password = "password";
            this.GlobalMeister =
            new StacheMeister(appId,
                              filename,
                              password,
                              StashPlan.spSerialize,
                              null,
                              null,
                              this.services);

            this.services.AddSingleton<StacheMeister>(GlobalMeister);

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

        #region Test Objects

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
                    Name = "Sales",
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
                        Country = "USA"
                    }
                },
            };
        }

        #endregion

        [TestMethod]
        public void _0_TestDBInsert()
        {
            string key = "test02";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            string s = "another test";
            s = JsonConvert.SerializeObject(s);

            stash.DbAddOrUpdate(new Stash() { Key = key, Value = s, Serialized = true });

            //stash.DB.Insert(new Stash() { key = key,  value = s, serialized=true });
            Task.Delay(1000);
            Stash result = stash.DbGet(key);
            Console.WriteLine("Payload Test: {0}", result.Value);
        }

        [TestMethod]
        public void _0_TestDBInsert_2()
        {
            string appId, filename, password;
            appId = "memstache.demo";
            filename = "MemstacheTest.cv";
            password = "password";
            string key = "test02";
            //StacheMeister Meister = new StacheMeister("memstache.demo");
            StacheMeister Meister = new StacheMeister(appId, filename, password);

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            string s = "another test";
            s = JsonConvert.SerializeObject(s);

            stash.DbAddOrUpdate(new Stash() { Key = key, Value = s, Serialized = true });

            //stash.DB.Insert(new Stash() { key = key,  value = s, serialized=true });
            Task.Delay(1000);
            Stash result = stash.DbGet(key);
            Console.WriteLine("Payload Test: {0}", result.Value);
        }


        [TestMethod]
        public void _0_InsertAndRetrieve()
        {
            string appId, filename, password;
            appId = "memstache.demo";
            filename = "MemstacheTest.cv";
            password = "password";
            string key = "test02";
            //StacheMeister Meister = new StacheMeister("memstache.demo");
            StacheMeister Meister = new StacheMeister(appId, filename, password,StashPlan.spProtectCompress);

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);

            Meister[key] = "testing"; //this.CreateEmployee();

            //stash.DB.Insert(new Stash() { key = key,  value = s, serialized=true });
            Task.Delay(10).Wait();
            string result = Meister[key];
            if (result != null)
                Console.WriteLine("1st Payload Test: {0}", result); //result.Id.ToString());

            Task.Delay(10).Wait();
            result = Meister[key];
            if(result != null)
                Console.WriteLine("2nd Payload Test: {0}", result); //result.Id.ToString());

            Task.Delay(10).Wait();
            result = Meister[key];
            if (result != null)
                Console.WriteLine("3rd Payload Test: {0}", result); //result.Id.ToString());
        }


        [TestMethod]
        public void _0_TestStasher()
        {
            string key = "test01";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            var payload = new Stash() { Key = key, Object = "This is a test" };

            var hash1 = Stasher.Hash(payload.Value);

            stash[key] = payload;

            payload.Value = "";

            payload = stash[key];

            Console.WriteLine("Payload Test: {0}", payload.Value);

            var hash2 = Stasher.Hash(payload.Value);
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void _0_TestHash()
        {
            for (int i = 0; i < 999; i++)
            {
                Random rnd = new Random();
                int length = 20;
                var str = string.Empty;
                for (var ii = 0; ii < length; ii++)
                {
                    str += ((char)(rnd.Next(1, 26) + 64)).ToString();
                }
                var h = Stasher.Hash(str);
                var h2 = Stasher.Hash(str);
                Assert.AreEqual(h, h2);
            }
        }

        public string GetFileName(string userId, string emailAddress, string pwHash)
        {
            //string s = userId + "_" + emailAddress + "_" + pwHash;
            string s = userId + "_" + pwHash;
            s = "File_" + Stasher.Hash(s).Replace("-", string.Empty) + ".cv";
            return s;
        }

        [TestMethod]
        public void _0_TestHashFileName()
        {
            string userId = "Tester01";
            string eml = "tester@test.com";
            for (int i = 0; i < 9; i++)
            {
                Random rnd = new Random();
                int length = 20;
                var str = string.Empty;
                for (var ii = 0; ii < length; ii++)
                {
                    str += ((char)(rnd.Next(1, 26) + 64)).ToString();
                }
                var h = Stasher.Hash(str);

                Console.WriteLine( this.GetFileName(userId, eml, h));
            }
        }
        /// <summary>
        /// StacheMeister should be able to serialize anything that NewtonSoft can handle.
        /// When in doubt test the serializatin here
        /// </summary>
        [TestMethod]
        public void _0_TestNewtonSoft()
        {
            DateTime dt = DateTime.UtcNow;
            long dtFileTimeUTC = dt.ToFileTimeUtc();
            bool b = true;
            int i = 1;
            double dbl = 10.10;
            float flt = 10.10F;
            string str = "string";

            string strSerialized = string.Empty;
            dynamic dyn;
            strSerialized = JsonConvert.SerializeObject(dt);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(DateTime));

            strSerialized = JsonConvert.SerializeObject(dtFileTimeUTC);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(long));

            strSerialized = JsonConvert.SerializeObject(b);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(bool));

            strSerialized = JsonConvert.SerializeObject(i);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(int));

            strSerialized = JsonConvert.SerializeObject(dbl);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(double));

            strSerialized = JsonConvert.SerializeObject(flt);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(float));

            strSerialized = JsonConvert.SerializeObject(str);
            dyn = JsonConvert.DeserializeObject(strSerialized, typeof(string));
        }

        [TestMethod]
        public void _0_GenVerificationCode()
        {
            for (int i = 0; i < 9; i++)
            {
                Random rnd = new Random();
                int length = 5;
                var str = string.Empty;
                for (var ii = 0; ii < length; ii++)
                {
                    str += ((char)(rnd.Next(1, 26) + 64)).ToString();
                }
                var h = Stasher.Hash(str).Replace("-", string.Empty).ToUpper().Substring(0, 5);
                //return h;

                Console.WriteLine(h); 
            }
        }

        [TestMethod]
        [TestCategory("Using Stasher")]
        public void _1_StasherSerialize()
        {
            string key = "test03";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { Key = key, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.Value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
            {
                Console.WriteLine("Payload is nulls");
            }

            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.Value);

            var hash2 = Stasher.Hash(payload.Value);
            Assert.AreEqual(hash1, hash2);
        }
        [TestMethod]
        [TestCategory("Using Stasher")]
        public void _2_StasherSerializeAndCompress()
        {
            string key = "test04";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerializeCompress);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { Key = key, StashPlan = StashPlan.spSerializeCompress, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.Value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
            {
                Console.WriteLine("Payload is null");
            }

            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.Value);

            var hash2 = Stasher.Hash(payload.Value);
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        [TestCategory("Using Stasher")]
        public void _3_StasherSerializeAndCompressAndEncrypt()
        {
            string key = "test05";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);
            Stasher stash = Meister.MakeStasher("test", StashPlan.spProtectCompress);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            Valuation valuation1 = new Valuation();

            var payload = new Stash() { Key = key, StashPlan = StashPlan.spProtectCompress, Object = valuation1 };
            var typeName = payload.StoredType;

            var hash1 = Stasher.Hash(payload.Value);

            stash[key] = payload;

            //payload.value = "";
            if (payload == null)
            {
                Console.WriteLine("Payload is nulls");
            }

            payload = stash[key];

            Valuation valuation2 = payload.Object as Valuation;

            Console.WriteLine("Payload Test: {0}", payload.Value);

            var hash2 = Stasher.Hash(payload.Value);
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        [TestCategory("Using StacheMeister")]
        public void _4_StacheMeisterSerialization()
        {
            string key = "test06";
            StacheMeister Meister = new StacheMeister("memstache.demo");

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);

            Employee emp1 = employee1;//CreateEmployee();
            string v1 = JsonConvert.SerializeObject(emp1);

            Meister[key] = emp1;

            Employee emp2 = Meister[key] as Employee;

            string v2 = JsonConvert.SerializeObject(emp2);

            Assert.AreEqual(v1, v2);
        }

        [TestMethod]
        [TestCategory("Using StacheMeister")]
        public void _5_StacheMeisterSerializeAndCompress()
        {
            string key = "test07";
            StacheMeister Meister = new StacheMeister("memstache.demo", null, null, StashPlan.spSerializeCompress);

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);

            Employee emp1 = employee1;//CreateEmployee();
            string v1 = JsonConvert.SerializeObject(emp1);

            Meister[key] = emp1;

            Employee emp2 = Meister[key] as Employee;

            string v2 = JsonConvert.SerializeObject(emp2);

            Assert.AreEqual(v1, v2);
        }

        [TestMethod]
        [TestCategory("Using StacheMeister")]
        public void _6_StacheMeisterSerializeAndCompressAndEncrypt()
        {
            string key = "test08";
            StacheMeister Meister = new StacheMeister("memstache.demo", null, null, StashPlan.spProtectCompress);

            //var rowcount = Meister.DB.Delete<Stash>(key);
            StashRepo.Delete(key);

            Employee emp1 = employee1;//CreateEmployee();
            string v1 = JsonConvert.SerializeObject(emp1);

            Meister[key] = emp1;

            Employee emp2 = Meister[key] as Employee;

            string v2 = JsonConvert.SerializeObject(emp2);

            Assert.AreEqual(v1, v2);
        }

        [TestMethod]
        [TestCategory("Using ServiceCollection")]
        public void _7_DependencyInjection()
        {


            var provider = services.BuildServiceProvider();
            using (var meister = provider.GetService<StacheMeister>())
            {
                string key = "test07";

                StashRepo.Delete(key);

                Employee emp1 = employee1;
                string v1 = JsonConvert.SerializeObject(emp1);

                meister[key] = emp1;

                Employee emp2 = meister[key] as Employee;

                string v2 = JsonConvert.SerializeObject(emp2);

                Assert.AreEqual(v1, v2);
            }
        }
    }
}
