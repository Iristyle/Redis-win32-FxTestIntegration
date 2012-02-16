![Redis-Logo](http://redis.io/images/redis.png)

# RedisIntegration 

A very simple .NET based client library for starting up an ephemeral [Redis](http://redis.io/) server from code during a test.

The goal here is simply to make it easier to code against a local Redis instance in tests (that can be on a Windows dev box or Windows build server) without having an official Redis server installed anywhere.

Usage is... simple.  When your test suite fires up, you can do the following to ensure you have a Redis instance running on localhost on port 6379, the Redis default.  This is best done in a static initalizer.

```csharp
var connectionInfo = RedisIntegration.HostManager.RunInstance();
```

If you want to control the port, use the overload

```csharp
var connectionInfo = RedisIntegration.HostManager.RunInstance(1235);
```

There is also a ```RunInstanceWithVisibleWindow``` overload so that you can see Redis connection info in a window.  You shouldn't need this if you're writing proper tests, but there you go.

# What does it do?

* Checks for the bitness of your processor, and launches the x86 or x64 binary as appropriate
* Writes a new randomly named config file, setting the port, pointing to a %temp% db file, and setting number of dbs to 1
* Creates an empty randomly named db file
* Launches the server
* Connects to send it a [FLUSHALL](http://redis.io/commands/flushall)

# Redis Version

The current Windows binaries are based on Redis 2.4.5.

# Thanks

Obviously this wouldn't be possible without the hard work of Antirez on the original [Redis](https://github.com/antirez/redis) and dmajkic on the [Win32 / Win64 port](https://github.com/dmajkic/redis/).  Redis is an absolutely fantastic piece of software.

# Warranties

* Don't call multiple times in a single test suite, as trying to run another instance on a locked port will throw

# TODO

* The code needs to take into account the potential for multiple test suites to be running on a build server at any given time
* There may need to be some additions made to ensure that Windows firewall ports are opened
* Think about randomizing the ports / ensuring Redis can grab the port / etc - a little thought should go into establishing a simple scheme for this.

# License

The .NET code is MIT license - do what you want with it.

In accordance with the original Redis license, those compiled bits are subject to the Redis [license](https://github.com/EastPoint/Redis-win32-FxTestIntegration/blob/master/src/RedisIntegration/tools/COPYING)