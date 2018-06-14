using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemStache;
using System.Diagnostics;
using System;

//using Stache = MemStache.MemStache<string, MemStache.IMemStacheProtectedItem<string>>;
using Stache = MemStache.MemStache<string, MemStache.MemStacheProtectedItem<string>>;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

            Console.WriteLine("TIME ELAPSED: " + elapsedTime );
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
            Stasher stash = Meister.MakeStasher("test",StashPlan.spSerialize);
            Console.WriteLine("Purpose: {0}",stash.Purpose);
        }

        [TestMethod]
        public void TestSerialization()
        {
            StacheMeister Meister = new StacheMeister("memstache.demo");
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            var payload = new Stash() { key = "test01", value="This is a test" };

            var hash1 = Stasher.Hash(payload.value);

            stash["test01"] = payload;

            payload.value = "";

            payload = stash["test01"];

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1,hash2);

        }

        [TestMethod]
        public void TestDBInsert()
        {
            StacheMeister Meister = new StacheMeister("memstache.demo");
            Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            string s = "another test";
            s = JsonConvert.SerializeObject(s);
            string key = "test02";
            stash.DbAddOrUpdate(new Stash() { key = key, value = s, serialized = true });
            //stash.DB.Insert(new Stash() { key = key,  value = s, serialized=true });
            Task.Delay(1000);
            Stash result = stash.DbGet(key);
            Console.WriteLine("Payload Test: {0}", result.value);
        }

        [TestMethod]
        public void TestMeisterStaches()
        {
            StacheMeister Meister = new StacheMeister("memstache.demo");
            //Stasher stash = Meister.MakeStasher("test", StashPlan.spSerialize);
            //Console.WriteLine("MemStache Initialized: {0}", stash.Purpose);

            var payload = new Stash() { key = "test01", value = "This is a test" };

            var hash1 = Stasher.Hash(payload.value);

            Meister.Serialized["test01"] = payload;

            //payload.value = "";

            payload = Meister.Serialized["test01"];

            Console.WriteLine("Payload Test: {0}", payload.value);

            var hash2 = Stasher.Hash(payload.value);
            Assert.AreEqual(hash1, hash2);
        }


    }
}
