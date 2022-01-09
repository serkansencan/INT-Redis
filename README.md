
INT-Redis
============

[![CI](https://github.com/automapper/automapper/workflows/CI/badge.svg)](https://github.com/serkansencan/INT-Redis)
[![NuGet](http://img.shields.io/nuget/vpre/AutoMapper.svg?label=NuGet)](https://www.nuget.org/packages/PTunes.Redis/)


### What is Redis?

INT-Redis is a high performance general purpose redis client for .NET languages (C#, etc.).


### How do I get started?

First, configure Int-Redis to know what types you want to map, in the startup of your application:

```csharp
using INT.Redis.Extensions;

public void ConfigureServices(IServiceCollection services)
{
	services.AddRedisCache(redisSettings =>
            {
                redisSettings.ConnectionString = "Redis connection string";
            });
}
```


Then in your application code, execute the mappings:

```csharp
using INT.Redis;

private readonly IRedisCacheManager _cacheManager;

public IndexModel(IRedisCacheManager cacheManager)
{
	_cacheManager = cacheManager;
}

public void CacheTutorial()
{
	Foo foo = new Foo();
	foo.Name = "Turkish delight";

	//Set foo Redis cache
	_cacheManager.Set("Key_Value", foo);

	//Set foo Redis cache expiry time
	_cacheManager.Set("Key_Value", foo, TimeSpan.FromMinutes(60));

	//Get foo data from Redis cache
	var fooItem = _cacheManager.Get<Foo>("Key_Value");

	//Check data in cache
	var isSetKey = _cacheManager.IsSet("Key_Value");

	//Get foo data list from Redis cache
	var fooList = _cacheManager.GetMany<Foo>("Key_Value");

	//Get foo data expiry time from Redis cache
	var fooCacheGetKeyRemainingTimeout = _cacheManager.GetKeyRemainingTimeout("Key_Value");

	//Remove foo data from Redia cache
	_cacheManager.Remove("Key_Value");
}
```


### Where can I get it?

First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [AutoMapper](https://www.nuget.org/packages/PTunes.Redis/) from the package manager console:

```
PM> Install-Package PTunes.Redis
```


### License, etc.

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

INT--Redis is Copyright &copy; 2022 [Serkan Şencan](https://github.com/serkansencan) and other contributors under the [MIT license](LICENSE.txt).

