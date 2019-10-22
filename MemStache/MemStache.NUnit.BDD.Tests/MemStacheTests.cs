using MemStache;
using MemStache.LiteDB;
using Newtonsoft.Json;
using NUnit.Framework;
using SpecEasy;
using System;
using System.Globalization;
using System.Threading;

namespace MemStache.Specs
{
    public class Valuation
    {
        public int Id { get; set; } = 100;

        public int StockId { get; set; } = 1;

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public decimal Price { get; set; } = 100.34m;
    }
    public class MemStacheSerializeSpec : Spec<MemStacheClientImpl>
    {
        public void Run()
        {
            object input = 0;
            When("running Memstache with StashPlan:Serialize", () => SUT.Run(input, StashPlan.spSerialize));
            Given("spSerialize: Integer Input of 1", () => input = 1).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerialize: String Input of 'Vote Now!'", () => input = "Vote Now!").Verify(() =>
                Then("spSerialize: input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerialize: DateTime Input of UtcNow", () => input = DateTime.UtcNow).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerialize: Object Input of Class Evaluation", () => input = new Valuation() { StockId = 999 }).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerialize: Object Graph Input of Class Employee", () => input = SUT.CreateEmployee()).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
        }
    }

    public class MemStacheCompressSpec : Spec<MemStacheClientImpl>
    {
        public void Run()
        {
            object input = 0;
            When("running Memstache with StashPlan:SerializeCompress", () => SUT.Run(input, StashPlan.spSerializeCompress));
            Given("spSerializeCompress: Integer Input of 1", () => input = 1).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerializeCompress: String Input of 'Vote Now!'", () => input = "Vote Now!").Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerializeCompress: DateTime Input of UtcNow", () => input = DateTime.UtcNow).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerializeCompress: Object Input of Class Evaluation", () => input = new Valuation() { StockId = 999 }).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spSerializeCompress: Object Graph Input of Class Employee", () => input = SUT.CreateEmployee()).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
        }
    }

    public class MemStacheEncryptSpec : Spec<MemStacheClientImpl>
    {
        public void Run()
        {
            object input = 0;

            When("running Memstache with StashPlan:ProtectCompress", () => SUT.Run(input, StashPlan.spProtectCompress));
            Given("spProtectCompress: Integer Input of 1", () => input = 1).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spProtectCompress: String Input of 'Vote Now!'", () => input = "Vote Now!").Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spProtectCompress: DateTime Input of UtcNow", () => input = DateTime.UtcNow).Verify(() =>
                Then("input and output JSON should match", () =>
                    Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spProtectCompress: Object Input of Class Evaluation", () => input = new Valuation() { StockId = 999 }).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
            Given("spProtectCompress: Object Graph Input of Class Employee", () => input = SUT.CreateEmployee()).Verify(() =>
                    Then("input and output JSON should match", () =>
                        Assert.That(SUT.Output, Is.EqualTo(JsonConvert.SerializeObject(input)))));
        }
    }

    public class MemStacheClientImpl
    {

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
                        Country = "USA",
                    },
                },
            };
        }

        public StacheMeister Cache { get; set; }
        public StashPlan plan { get; set; }
        public string Key { get; set; }
        public string Output { get; set; }
        public void Run(object input, StashPlan plan)
        {
            Key = "TEST";
            Cache = new StacheMeister("memstache.demo", null, null, plan);
            StashRepo.Delete(Key);
            Cache[Key] = input;
            Thread.Sleep(1);
            object output = Cache[Key];
            if (output == null)
                Output = null;
            else
                Output = JsonConvert.SerializeObject(output);
        }

    }

}
