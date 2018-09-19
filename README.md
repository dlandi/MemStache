# MemStache
A Flexible, Powerful Cache Library with these features

		- In-Memory Resiliency (memory management)
		- Serialization
		- Data Persistence
		- Instant Data Availability 
		- Security In-Memory (Encrypted Memory)
		- Security On-Disk (Encrypted file on disk)
		- All Code written in .NET Standard 2.
		- Leverage .NET Standard DI Framework.
		- A Small Intuitive Developer API
![Mem Stache Class Diagram](MemStacheClassDiagram.png)

MemStache is designed to be used exactly the way the original Asp.NET Memory Cache was used.

Example -
    Original Asp.Net code: 
```
        MemoryCache cache = MemoryCache.Default;
        cache[EmployeeId] = CreateEmployee(EmployeeId, name, dob);
        Employee empResult = (Employee)cache[EmployeeId];
```
    MemStache equivalent: 
```
        StacheMeister cache = new StacheMeister("memstache.demo", "FileName", "Password", StashPlan.spProtectCompress);
        cache[EmployeeId] = CreateEmployee(EmployeeId, name, dob);
        Employee empResult = Meister[key] as Employee;
```
Same code semantics, but Memstache utilizes:

		- the NewtonSoft library to serialize the data, 
		- the .NET Standard 2 ASPNET.Core.DataProtection API to encrypt the data in-memory
		- Microsoft.Extensions.Caching.Memory API to provide a memory-managed in-memory Cache
		- the LiteDB library to provide an encrypted on-disk Cache

I would like to acknowledge James Montemagno and Frank Krueger and their podcast on Monkey Cache for giving me the inpsiration to write my own cache library:
https://www.mergeconflict.fm/76

I also based my implementation of LiteDB integration on the one in MonkeyCache!  Thanks!


Enjoy
