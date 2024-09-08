# MemStache (THIS VERSION IS OBSOLETE)
A Flexible, Powerful Cache Library with these features

		- In-Memory Resiliency (memory management)
		- Serialization
		- Data Persistence
		- Instant Data Availability 
		- Security In-Memory (Encrypted Memory)
		- Security On-Disk (Encrypted file on disk)
		- All Code written in .NET 6.
		- Leverage Standard .NET Core DI Framework.
		- A Small Intuitive Developer API

Nuget Package is available: https://www.nuget.org/packages/MemStache

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
        Employee empResult = cache[EmployeeId] as Employee;
```
Same code semantics, but Memstache utilizes:

		- the NewtonSoft library to serialize the data, 
		- built-in data-compression
		- the Microsoft.AspNetCore.DataProtection API to encrypt the data in-memory
		- Microsoft.Extensions.Caching.Memory API to provide a memory-managed in-memory Cache
		- the LiteDB library to provide an encrypted on-disk Cache
		- optional use of compression and/or encryption.        

Enjoy
