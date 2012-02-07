# RedisIntegration 
A very simple .NET based client library for starting up a ephemeral Redis server from code during a test.

The goal here is simply to make it easier to code against a local Redis instance in tests (that can be on a Windows dev box or Windows build server) without having an official Redis server installed anywhere.

Usage is... simple.  When your test suite fires up, you can do the following to ensure you have a Redis instance running on localhost on port 6379, the Redis default.  This is best done in a static initalizer.

```csharp
RedisIntegration.HostManager.RunInstance();
```

If you want to control the port, use the overload

```csharp
RedisIntegration.HostManager.RunInstance(1235);
```

There is also a ```RunInstanceWithVisibleWindow``` overload so that you can see Redis connection info in a window.  You shouldn't need this if you're writing proper tests, but there you go.


# Warranties

* Don't call multiple times in a single test suite, as trying to run another instance on a locked port will throw
